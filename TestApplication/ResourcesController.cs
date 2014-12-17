using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebShard;

namespace TestApplication
{
    public class ResourcesController
    {
        public IResponse Get(string resourceName, string resourcePath)
        {
            return new FileSystemResponse("Content/" + resourcePath + "/" + resourceName, resourceName.EndsWith("js") ? "application/javascript" : "application/css", "utf-8");
        }
    }
}
