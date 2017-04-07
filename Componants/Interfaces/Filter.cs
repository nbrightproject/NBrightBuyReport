using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using DotNetNuke.Entities.Portals;
using NBrightDNN;
using Nevoweb.DNN.NBrightBuy.Components;

namespace Nevoweb.DNN.NBrightBuy.Providers.NBrightBuyReport
{
    public class Filter : Components.Interfaces.FilterInterface
    {
        public override string GetFilter(string currentFilter, NavigationData navigationData, ModSettings setting, HttpContext context)
        {
            throw new NotImplementedException();
        }
    }
}
