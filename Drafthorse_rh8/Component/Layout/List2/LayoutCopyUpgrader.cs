using Grasshopper.Kernel;
using System;

namespace Drafthorse_rh8.Component.Layout.List2
{
    internal class LayoutCopyUpgrader: IGH_UpgradeObject
    {
        public Guid UpgradeFrom => new Guid("{f1d0ba78-7f55-4bed-a60d-eb46b5770c9e}");

        public Guid UpgradeTo => new Guid("{35457848-2D5F-47AD-8118-8E7586430DB7}");

        public DateTime Version => new DateTime(2023, 11, 24, 13, 11, 0);

        public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
        {
            if (!(target is IGH_Component oldComponent))
            {
                return null;
            }
            IGH_Component iGH_Component = GH_UpgradeUtil.SwapComponents(oldComponent, UpgradeTo);
            return iGH_Component;
        }
    }
}
