using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OPA.ContinuousAlgorithms
{
    public class HarmonySearch : MultiTrajectoryContinuousSolver
    {
        protected int mDimension;
        protected int mMemorySize;

        protected double mConsolidationRate;
        protected double mPitchAdjustRate;

        public delegate ContinuousSolution CreateSolutionMethod(object constraints);
        protected CreateSolutionMethod mSolutionGenerator;

        public double ConsidationRate
        {
            get { return mConsolidationRate; }
            set { mConsolidationRate = value; }
        }

        public double PitchAdjustRate
        {
            get { return mPitchAdjustRate; }
            set { mPitchAdjustRate = value; }
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
                    double lower_bound=mLowerBounds[i];
                    double upper_bound=mUpperBounds[i];
                    solution[i] = lower_bound + (upper_bound - lower_bound) * RandomEngine.NextDouble();
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
            

            return solution;
        }

        public HarmonySearch(int memory_size, int dimension, CreateSolutionMethod generator)
        {
            mMemorySize = memory_size;
            mDimension = dimension;

            mSolutionGenerator = generator;
            if (mSolutionGenerator == null)
            {
                throw new NullReferenceException();
            }
        }

        public override ContinuousSolution Minimize(CostEvaluationMethod evaluate, GradientEvaluationMethod calc_gradient, TerminationEvaluationMethod should_terminate, object constraints = null)
        {
            List<ContinuousSolution> memory = new List<ContinuousSolution>();

            for (int i = 0; i < mMemorySize; ++i)
            {
                ContinuousSolution s = CreateInitSolution(mSolutionGenerator, constraints);
                s.Cost = evaluate(s.Values, mLowerBounds, mUpperBounds, constraints);
            }

            double? improvement = null;
            int iteration = 0;

            memory=memory.OrderBy(s => s.Cost).ToList();

            ContinuousSolution best_solution = memory[0].Clone() as ContinuousSolution;

            while (!should_terminate(improvement, iteration))
            {
                ContinuousSolution harmony = CreateHarmony(memory, mConsolidationRate, mPitchAdjustRate, constraints);

                harmony.Cost = evaluate(harmony.Values, mLowerBounds, mUpperBounds, constraints);

                memory.Add(harmony);

                var sorted_result = memory.OrderBy(s => s.Cost);

                List<ContinuousSolution> new_memory = new List<ContinuousSolution>();

                foreach (ContinuousSolution s in sorted_result)
                {
                    if (memory.Count == mMemorySize) break;
                    new_memory.Add(s);
                }

                memory = new_memory;

                if (best_solution.TryUpdateSolution(memory[0].Values, memory[0].Cost, out improvement))
                {
                    OnSolutionUpdated(best_solution, iteration);
                }

                OnStepped(best_solution, iteration);
                iteration++;
            }

            return best_solution;
        }

        protected ContinuousSolution CreateHarmony(List<ContinuousSolution> memory, double coonsidation_rate, double pitch_adjust_rate, object constraints)
        {
            double[] x = new double[mDimension];

            double[] lower_bounds = null;
            double[] upper_bounds = null;

            if (mLowerBounds == null || mUpperBounds == null)
            {
                if (constraints != null && constraints is Tuple<double[], double[]>)
                {
                    Tuple<double[], double[]> bounds = constraints as Tuple<double[], double[]>;
                    lower_bounds = bounds.Item1;
                    upper_bounds = bounds.Item2;
                }
                else
                {
                    throw new ArgumentNullException();
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

            for (int i = 0; i < mDimension; ++i)
            {
                if (RandomEngine.NextDouble() < mConsolidationRate)
                {
                    x[i] = memory[RandomEngine.NextInt(memory.Count)][i];
                    if (RandomEngine.NextDouble() < mPitchAdjustRate)
                    {
                        x[i] += (-1 + 2 * RandomEngine.NextDouble());
                        x[i] = System.Math.Min(upper_bounds[i], x[i]);
                        x[i] = System.Math.Max(lower_bounds[i], x[i]);
                    }
                }
                else
                {
                    x[i] = RandomEngine.NextBoolean() ? 1 : 0;
                }
            }

            return new ContinuousSolution(x, double.MaxValue);
        }
    }
}
