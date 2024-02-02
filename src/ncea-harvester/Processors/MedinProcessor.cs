using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ncea.harvester.Processors
{
    public interface IMedinProcessor
    {
        Task Process();
    }
    public class MedinProcessor : IMedinProcessor
    {
        public Task Process()
        {
            throw new NotImplementedException();
        }
    }
}
