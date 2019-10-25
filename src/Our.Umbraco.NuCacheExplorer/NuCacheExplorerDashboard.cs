using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Dashboards;

namespace Our.Umbraco.NuCacheExplorer
{
    [Weight(60)]
    public class NuCacheExplorerDashboard : IDashboard
    {
        public string Alias => "NuCacheExplorerDashboard";

        public string[] Sections => new[] { "settings" };

        public string View => "/App_Plugins/OurUmbracoNuCacheExplorer/dashboard.html";

        public IAccessRule[] AccessRules
        {
            get
            {
                var rules = new IAccessRule[]
                {
                    new AccessRule {Type = AccessRuleType.Grant, Value = Constants.Security.AdminGroupAlias}
                };
                return rules;
            }
        }
    }
}