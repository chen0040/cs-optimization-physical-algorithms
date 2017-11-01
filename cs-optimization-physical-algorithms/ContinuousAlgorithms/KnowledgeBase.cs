using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OPA.ContinuousAlgorithms
{
    public class KnowledgeBase
    {
        protected ContinuousSolution mSituationalKnowledge;

        public ContinuousSolution SituationalKnowledge
        {
            get { return mSituationalKnowledge; }
            set { mSituationalKnowledge = value; }
        }

        protected double[] mNormativeKnowledge_LowerBounds = null;
        protected double[] mNormativeKnowledge_UpperBounds = null;

        protected int mDimension;

        public KnowledgeBase(int dimension, double[] lower_bounds, double[] upper_bounds)
        {
            mNormativeKnowledge_LowerBounds = (double[])lower_bounds.Clone();
            mNormativeKnowledge_UpperBounds = (double[])upper_bounds.Clone();

            mDimension = dimension;
        }

        public bool UpdateSituationalKnowledge(ContinuousSolution best_situational_knowledge, out double? improvement)
        {
            improvement = null;
            if (mSituationalKnowledge == null)
            {
                mSituationalKnowledge = best_situational_knowledge;
                return true;
            }
            else
            {
                return mSituationalKnowledge.TryUpdateSolution(best_situational_knowledge.Values, best_situational_knowledge.Cost, out improvement);
            }
        }

        public double InitWithinNormativeKnowledge(int index)
        {
            double lower_bound=mNormativeKnowledge_LowerBounds[index];
            double upper_bound=mNormativeKnowledge_UpperBounds[index];

            return lower_bound + (upper_bound - lower_bound) * RandomEngine.NextDouble();
        }

        public void UpdateNormativeKnowledge(IEnumerable<ContinuousSolution> best_normative_knowledge)
        {
            for (int i = 0; i < mDimension; ++i)
            {
                double lower_bound = double.MaxValue;
                double upper_bound = double.MinValue;
                foreach (ContinuousSolution s in best_normative_knowledge)
                {
                    lower_bound = System.Math.Min(s[i], lower_bound);
                    upper_bound = System.Math.Max(s[i], upper_bound);
                }
                mNormativeKnowledge_LowerBounds[i] = lower_bound;
                mNormativeKnowledge_UpperBounds[i] = upper_bound;
            }
        }

        public double[] NormativeKnowledge_LowerBounds
        {
            get { return mNormativeKnowledge_LowerBounds; }
        }

        public double[] NormativeKnowledge_UpperBounds
        {
            get { return mNormativeKnowledge_UpperBounds; }
        }
    }
}
