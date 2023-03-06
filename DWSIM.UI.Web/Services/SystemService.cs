using DWSIM.UI.Web.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace DWSIM.UI.Web.Services
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class SystemService
    {
        public void OpenUrlInLocalBrowser(string url)
        {
            Process.Start(url);
        }
        public string GetUrlSettings()
        {


#if S365_STAGING
            return JsonConvert.SerializeObject(new UrlSettingsResponseModel
            {
                DashboardServiceUrl = "https://s365-dashboard-v2-service-staging.azurewebsites.net",
                ExcelRunnerServiceUrl = "https://excel-runner-staging.azurewebsites.net",
                SensitivityStudiesServiceUrl = "https://sensitivity-studies-staging.azurewebsites.net",
                TakeHomeExamsServiceUrl = "https://take-home-exams-staging.azurewebsites.net"
            },new JsonSerializerSettings
{
    ContractResolver = new CamelCasePropertyNamesContractResolver()
});

#else
            return JsonConvert.SerializeObject(new UrlSettingsResponseModel
            {
                DashboardServiceUrl = "https://dashboard-service.simulate365.com",
                ExcelRunnerServiceUrl = "https://excel-runner.azurewebsites.net",
                SensitivityStudiesServiceUrl = "https://sensitivity-studies.azurewebsites.net",
                TakeHomeExamsServiceUrl = "https://take-home-exams.azurewebsites.net"
            }, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
#endif



        }
    }
}
