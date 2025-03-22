using System.Collections.Generic;
using Rhino;

namespace Drafthorse.Helper.PaperSize
{
    /// <summary>
    /// Represents a standard paper size with dimensions
    /// </summary>
    public class PaperSize
    {
        /// <summary>
        /// The display name of the paper size
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The width of the paper
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// The height of the paper
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// The unit system for this paper size (0=inches, 1=cm, 2=mm)
        /// Can be null if no specific unit is enforced
        /// </summary>
        public int? Units { get; set; }

        /// <summary>
        /// Creates a new paper size definition
        /// </summary>
        /// <param name="name">Display name</param>
        /// <param name="width">Paper width</param>
        /// <param name="height">Paper height</param>
        /// <param name="units">Units index (0=inches, 1=cm, 2=mm) or null</param>
        public PaperSize(string name, double width, double height, int? units = null)
        {
            Name = name;
            Width = width;
            Height = height;
            Units = units;
        }
    }

    /// <summary>
    /// Library of standard paper sizes
    /// </summary>
    public static class PaperSizeLibrary
    {
        /// <summary>
        /// Get standard imperial paper sizes (in inches)
        /// </summary>
        public static List<PaperSize> GetImperialPaperSizes()
        {
            return new List<PaperSize>
            {
                new PaperSize("Letter", 8.5, 11, 0),
                new PaperSize("Legal", 8.5, 14, 0),
                new PaperSize("Tabloid/Ledger", 11, 17, 0),
                new PaperSize("ANSI A", 8.5, 11, 0),
                new PaperSize("ANSI B", 11, 17, 0),
                new PaperSize("ANSI C", 17, 22, 0),
                new PaperSize("ANSI D", 22, 34, 0),
                new PaperSize("ANSI E", 34, 44, 0),
                new PaperSize("Arch A", 9, 12, 0),
                new PaperSize("Arch B", 12, 18, 0),
                new PaperSize("Arch C", 18, 24, 0),
                new PaperSize("Arch D", 24, 36, 0),
                new PaperSize("Arch E", 36, 48, 0),
                // Common metric sizes converted to inches
                new PaperSize("A4 (Metric)", 8.27, 11.69, 0),
                new PaperSize("A3 (Metric)", 11.69, 16.54, 0),
                new PaperSize("A2 (Metric)", 16.54, 23.39, 0),
                new PaperSize("A1 (Metric)", 23.39, 33.11, 0)
            };
        }

        /// <summary>
        /// Get standard metric paper sizes (in mm)
        /// </summary>
        public static List<PaperSize> GetMetricPaperSizes()
        {
            return new List<PaperSize>
            {
                new PaperSize("A0", 841, 1189, 2),
                new PaperSize("A1", 594, 841, 2),
                new PaperSize("A2", 420, 594, 2),
                new PaperSize("A3", 297, 420, 2),
                new PaperSize("A4", 210, 297, 2),
                new PaperSize("A5", 148, 210, 2),
                new PaperSize("B0", 1000, 1414, 2),
                new PaperSize("B1", 707, 1000, 2),
                new PaperSize("B2", 500, 707, 2),
                new PaperSize("B3", 353, 500, 2),
                new PaperSize("B4", 250, 353, 2),
                new PaperSize("B5", 176, 250, 2),
                // Common imperial sizes converted to mm
                new PaperSize("Letter (US)", 216, 279, 2),
                new PaperSize("Tabloid (US)", 279, 432, 2),
                new PaperSize("ANSI B (US)", 279, 432, 2)
            };
        }

        /// <summary>
        /// Get paper sizes appropriate for the given unit system
        /// </summary>
        public static List<PaperSize> GetPaperSizesForUnitSystem(UnitSystem unitSystem)
        {
            string unitName = unitSystem.ToString().ToLowerInvariant();
            bool isMetric = unitName.EndsWith("meters");

            return isMetric ? GetMetricPaperSizes() : GetImperialPaperSizes();
        }
    }
}