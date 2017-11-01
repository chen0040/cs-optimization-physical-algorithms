using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OPA.BinaryAlgorithms
{
    public class SimulatedAnnealing : StochasticHillClimber
    {
        protected double mT0 = 100;
        protected int mMCSteps = 100;
        protected double mCoolingRate = 0.99;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="masks"></param>
        /// <param name="T0">initial temperature</param>
        public SimulatedAnnealing(int[] masks, double T0, int monte_carlo_steps, double cooling_rate)
            : base(masks)
        {
            mMCSteps = monte_carlo_steps;
            mT0 = T0;
            mCoolingRate = cooling_rate;
        }

        public double T0
        {
            get { return mT0; }
            set { mT0 = value; }
        }

        public int MonteCarloSteps
        {
            get { return mMCSteps; }
            set { mMCSteps = value; }
        }

        public double CoolingRate
        {
            get { return mCoolingRate; }
            set { mCoolingRate = value; }
        }

        protected virtual void DecreaseTemperature(ref double T)
        {
            T = T * mCoolingRate;
        }

        public override BinarySolution Minimize(int[] x_0, CostEvaluationMethod evaluate, TerminationEvaluationMethod should_terminate, object constraints = null)
        {
            double? improvement = null;
            int iteration = 0;

            double fx_0 = evaluate(x_0, constraints);
            BinarySolution best_solution = new BinarySolution(x_0, fx_0);

            double T = mT0;

            int dimension = x_0.Length;

            while (!should_terminate(improvement, iteration))
            {
                for (int i = 0; i < mMCSteps; ++i)
                {
                    int[] x_pi = GetNeighbor(best_solution.Values, RandomEngine.NextInt(dimension), constraints);
                    double fx_pi = evaluate(x_pi, constraints);


                    if (best_solution.TryUpdateSolution(x_pi, fx_pi, out improvement))
                    {
                        OnSolutionUpdated(best_solution, iteration);
                    }
                    else
                    {
                        double P = System.Math.Exp((best_solution.Cost - fx_pi) / T);
                        double r = RandomEngine.NextDouble();

                        if (r <= P)
                        {
                            best_solution = new BinarySolution(x_pi, fx_pi);
                            OnSolutionUpdated(best_solution, iteration);
                        }
                    }
                }

                OnStepped(best_solution, iteration);

                DecreaseTemperature(ref T);
                iteration++;
            }

            return best_solution;
        }
    }
}
