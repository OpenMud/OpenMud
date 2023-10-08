using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.Compiler.Project.Cli
{
    public enum ProjectTask
    {
        //Host just the logic server
        Host,
        //Host logic server & client
        Debug,
        
        //Build client & server
        Build,

        //build client
        BuildAsset,

        //build logic
        BuildLogic,

        //Create a template client (if client not present)
        //Create a template logic (if dme not found.)
        Init
        //prompt: Project Name
    }
}
