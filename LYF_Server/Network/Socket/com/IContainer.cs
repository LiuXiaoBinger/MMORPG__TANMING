using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public interface IContainer
{

    void OnInit();


    void OnServerCommand(ServerBase serverBase, BasePackage basePackage);

    void OnClientCommand(ServerBase serverBase, BasePackage basePackage);

}
