using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OPA.ProblemModels;

namespace OPA.ComponentModels
{
    public interface ISolution
    {
        ISolution Clone();
    }
}
