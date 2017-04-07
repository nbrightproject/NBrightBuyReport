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
    public class Payments : Components.Interfaces.PaymentsInterface
    {
        public override string GetTemplate(NBrightInfo cartInfo)
        {
            throw new NotImplementedException();
        }

        public override string RedirectForPayment(OrderData orderData)
        {
            throw new NotImplementedException();
        }

        public override string ProcessPaymentReturn(HttpContext context)
        {
            throw new NotImplementedException();
        }

        public override string Paymentskey { get; set; }
    }
}
