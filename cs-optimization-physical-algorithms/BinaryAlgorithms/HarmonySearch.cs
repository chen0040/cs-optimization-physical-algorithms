using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OPA.BinaryAlgorithms
{
    public class HarmonySearch : MultiTrajectoryBinarySolver
    {
        protected int mDimension;
        protected int mMemorySize;

        protected double mConsolidationRate;
        protected double mPitchAdjustRate;

        public delegate BinarySolution CreateSolutionMethod(object constraints);
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

        protected BinarySolution CreateInitSolution(CreateSolutionMethod generator, object constraints)
        {
            if (generator != null)
            {
                return generator(constraints);
            }
            int[] x = new int[mDimension];
            BinarySolution solution = new BinarySolution(x, double.MaxValue);

            for (int i = 0; i < mDimension; ++i)
            {
                solution[i] = RandomEngine.NextBoolean() ? 1 : 0;
            }

            return solution;
        }

        public HarmonySearch(int memory_size, int dimension, CreateSolutionMethod solution_generator)
        {
            mMemorySize = memory_size;
            mDimension = dimension;

            mSolutionGenerator = solution_generator;
            if (mSolutionGenerator == null)
            {
                throw new NullReferenceException();
            }
        }

        public override BinarySolution Minimize(CostEvaluationMethod evaluate, TerminationEvaluationMethod should_terminate, object constraints = null)
        {
            List<BinarySolution> memory = new List<BinarySolution>();

            for (int i = 0; i < mMemorySize; ++i)
            {
                BinarySolution s = CreateInitSolution(mSolutionGenerator, constraints);
                s.Cost = evaluate(s.Values, constraints);
            }

            double? improvement = null;
            int iteration = 0;

            memory=memory.OrderBy(s => s.Cost).ToList();

            BinarySolution best_solution=memory[0].Clone() as BinarySolution;

            while (!should_terminate(improvement, iteration))
            {
                BinarySolution harmony = CreateHarmony(memory, mConsolidationRate, mPitchAdjustRate);

                harmony.Cost = evaluate(harmony.Values, constraints);

                memory.Add(harmony);

                var sorted_result = memory.OrderBy(s => s.Cost);

                List<BinarySolution> new_memory = new List<BinarySolution>();

                foreach (BinarySolution s in sorted_result)
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

        protected BinarySolution CreateHarmony(List<BinarySolution> memory, double coonsidation_rate, double pitch_adjust_rate)
        {
            int[] x = new int[mDimension];

            for (int i = 0; i < mDimension; ++i)
            {
                if (RandomEngine.NextDouble() < mConsolidationRate)
                {
                    x[i] = memory[RandomEngine.NextInt(memory.Count)][i];
                    if (RandomEngine.NextDouble() < mPitchAdjustRate)
                    {
                        x[i] = x[i] == 1 ? 0 : 1;
                    }
                }
                else
                {
                    x[i] = RandomEngine.NextBoolean() ? 1 : 0;
                }
            }

            return new BinarySolution(x, double.MaxValue);
        }
    }
}
