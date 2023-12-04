using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes
{
    public sealed class DmlUserException : Exception
    {
        public EnvObjectReference UserException;

        public DmlUserException(EnvObjectReference userException) {
            this.UserException = VarEnvObjectReference.CreateImmutable(userException);
        }
    }
}
