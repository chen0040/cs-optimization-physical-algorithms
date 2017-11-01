using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OPA.ContinuousAlgorithms
{
    /// <summary>
    /// CAEP implements the Cultural Algorithm Evolution Program
    /// </summary>
    public class CAEP : MultiTrajectoryContinuousSolver
    {
        protected int mPopSize;
        protected int mDimension;

        protected double mMutationRate = 0.5;

        protected int mNumAccepted = 10;

        public double MutationRate
        {
            get { return mMutationRate; }
            set { mMutationRate = value; }
        }

        public delegate ContinuousSolution CreateSolutionMethod(object constraints);
        protected CreateSolutionMethod mSolutionGenerator;

        public CAEP(int pop_size, int dimension, CreateSolutionMethod solution_generator)
        {
            mPopSize = pop_size;
            mDimension = dimension;

            mSolutionGenerator = solution_generator;
            if (mSolutionGenerator == null)
            {
                throw new NullReferenceException();
            }
        }

        protected ContinuousSolution CreateInitSolution(CreateSolutionMethod generator, object constraints)
        {
            if (generator != null)
            {
                return generator(constraints);
            }
            double[] x = new double[mDimension];
            ContinuousSolution solution = new ContinuousSolution(x, double.MaxValue);

            if (mLowerBounds != null && mUpperBounds != null)
            {
                for (int i = 0; i < mDimension; ++i)
                {
                    double lower_bound = mLowerBounds[i];
                    double upper_bound = mUpperBounds[i];
                    solution[i] = lower_bound + (upper_bound - lower_bound) * RandomEngine.NextDouble();
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }


            return solution;
        }

        protected ContinuousSolution MutateWithInfluence(ContinuousSolution parent, KnowledgeBase knowledge)
        {
            double[] x = new double[mDimension];
            for (int i = 0; i < mDimension; ++i)
            {
                x[i] = parent[i];
                if (RandomEngine.NextDouble() < mMutationRate)
                {
                    x[i] = knowledge.InitWithinNormativeKnowledge(i);
                }
            }

            return new ContinuousSolution(x, double.MaxValue);
        }

        protected ContinuousSolution BinaryTournamentSelection(List<ContinuousSolution> pop)
        {
            int pop_size = pop.Count;
            int index1 = RandomEngine.NextInt(pop_size);
            int index2=index1;
            do{
                index2=RandomEngine.NextInt(pop_size);
            }while(index1==index2);

            ContinuousSolution s1=pop[index1];
            ContinuousSolution s2=pop[index2];

            if(s1.IsBetterThan(s2))
            {
                return s1;
            }
            return s2;
            
        }

        public override ContinuousSolution Minimize(CostEvaluationMethod evaluate, GradientEvaluationMethod calc_grad, TerminationEvaluationMethod should_terminate, object constraints = null)
        {
            List<ContinuousSolution> pop = new List<ContinuousSolution>();

            for (int i = 0; i < mPopSize; ++i)
            {
                ContinuousSolution s = CreateInitSolution(mSolutionGenerator, constraints);
                s.Cost = evaluate(s.Values, mLowerBounds, mUpperBounds, constraints);
            }

            double? improvement = null;
            int iteration = 0;

            pop = pop.OrderBy(s => s.Cost).ToList();

            double[] lower_bounds=null;
            double[] upper_bounds=null;

            if (mLowerBounds == null || mUpperBounds == null)
            {
                if (constraints is Tuple<double[], double[]>)
                {
                    Tuple<double[], double[]> bounds = constraints as Tuple<double[], double[]>;
                    lower_bounds = bounds.Item1;
                    upper_bounds = bounds.Item2;
                }
                else
                {
                    throw new InvalidCastException();
                }
            }
            else
            {
                lower_bounds = mLowerBounds;
                upper_bounds = mUpperBounds;
            }

            if (lower_bounds.Length < mDimension)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (upper_bounds.Length < mDimension)
            {
                throw new ArgumentOutOfRangeException();
            }

            KnowledgeBase belief_space = new KnowledgeBase(mDimension, lower_bounds, upper_bounds);

            belief_space.UpdateSituationalKnowledge(pop[0].Clone() as ContinuousSolution, out improvement);

            ContinuousSolution[] children = new ContinuousSolution[mPopSize];
            while (!should_terminate(improvement, iteration))
            {
                for (int i = 0; i < mPopSize; ++i)
                {
                    children[i] = MutateWithInfluence(pop[i], belief_space);
                    children[i].Cost = evaluate(children[i].Values, lower_bounds, upper_bounds, constraints);
                }

                children = children.OrderBy(s => s.Cost).ToArray();

                if (belief_space.UpdateSituationalKnowledge(children[0], out improvement))
                {
                    OnSolutionUpdated(belief_space.SituationalKnowledge, iteration);
                }

                List<ContinuousSolution> inter_generation = new List<ContinuousSolution>();
                inter_generation.AddRange(pop);
                inter_generation.AddRange(children);

                for (int i = 0; i < mPopSize; ++i)
                {
                    pop[i] = BinaryTournamentSelection(inter_generation);
                }

                pop = pop.OrderBy(s => s.Cost).ToList();

                List<ContinuousSolution> best_solutions = new List<ContinuousSolution>();
                for (int i = 0; i < mNumAccepted; ++i)
                {
                    best_solutions.Add(pop[i]);
                }

                belief_space.UpdateNormativeKnowledge(best_solutions);

                OnStepped(belief_space.SituationalKnowledge, iteration);
                iteration++;
            }

            return belief_space.SituationalKnowledge;
        }
    }
}
