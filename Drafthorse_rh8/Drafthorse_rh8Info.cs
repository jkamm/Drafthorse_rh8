﻿using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Drafthorse
{
    public class Drafthorse_rh8Info : GH_AssemblyInfo
    {
        public override string Name => "DraftHorse";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => Drafthorse_rh8.Properties.Resources.DrafthorseLogo;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "Tools for managing Rhino Layouts programmatically through Grasshopper";

        public override Guid Id => new Guid("6b12ef31-6141-460f-a658-0f8da4ac5a22");

        //Return a string identifying you or your company.
        public override string AuthorName => "Jo Kamm";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "jokamm@gmail.com";
    }
}