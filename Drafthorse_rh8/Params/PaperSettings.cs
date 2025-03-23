using Rhino;
using System;
using System.Collections.Generic;

namespace Drafthorse.Params
{
    public class PaperSettings
    {
        // Name
        public string Name { get; set; }

        // Paper size
        public double Width { get; set; }
        public double Height { get; set; }

        // Units (mm, inches, etc.)
        public PaperUnit Unit { get; set; }

        // Orientation
        public PaperOrientation Orientation { get; set; }

        // Margins
        public double MarginTop { get; set; }
        public double MarginBottom { get; set; }
        public double MarginLeft { get; set; }
        public double MarginRight { get; set; }

        public PaperSettings()
        {
            //// Default settings (e.g., A4 portrait)
            //Name = "A4";
            //Width = 210;
            //Height = 297;
            //Unit = PaperUnit.Millimeters;
            //Orientation = PaperOrientation.Portrait;
            //MarginTop = MarginBottom = MarginLeft = MarginRight = 10;

        }


        /// <summary>
        /// Creates a new paper size definition
        /// </summary>
        /// <param name="name">Display name</param>
        /// <param name="width">Paper width</param>
        /// <param name="height">Paper height</param>
        /// <param name="units">Units
        public PaperSettings(string name, double width, double height, PaperUnit units)
        {
            Name = name;
            Width = width;
            Height = height;
            Unit = units;
            Orientation = PaperOrientation.Portrait;
            double margin = units == PaperUnit.Inches ? .5 : units == PaperUnit.Millimeters ? 10 : 1;
            MarginTop = MarginBottom = MarginLeft = MarginRight = margin;
        }

        public PaperSettings Duplicate()
        {
            return new PaperSettings
            {
                Name = this.Name,
                Width = this.Width,
                Height = this.Height,
                Unit = this.Unit,
                Orientation = this.Orientation,
                MarginTop = this.MarginTop,
                MarginBottom = this.MarginBottom,
                MarginLeft = this.MarginLeft,
                MarginRight = this.MarginRight,
            };
        }

        public override string ToString()
        {
            return $"{Name}: {Width}x{Height} {Unit}, {Orientation}";
        }

        /// <summary>
        /// Get whether RhinoDoc Page Units are Metric (true) or Imperial (false)
        /// </summary>
        /// <returns></returns>
        public bool RhinoDocPageUnitsAreMetric()
        { return false; }

        /// <summary>
        /// Gets whether this PaperSetting is metric (true) or Imperial (false) 
        /// </summary>
        /// <returns></returns>
        public bool IsMetric()
        {
            return Unit != PaperUnit.Inches;
        }

        public void SetUniformMargins(double margin)
        {
            // Add exception handling for if margin exceeds size of paper
            if (margin < 0) throw new Exception("Margin can't be less than 0");
            else if (margin * 2 > Width || margin * 2 > Height) throw new Exception("Margin exceeds page size");

            MarginBottom = MarginLeft = MarginRight = MarginTop = margin;
            
        }

        /// <summary>
        /// Explicitly converts a string paper name to a PaperSettings object
        /// </summary>
        /// <param name="paperName">Name of the paper to find</param>
        /// <returns>New PaperSettings object matching the requested name</returns>
        /// <exception cref="ArgumentException">Thrown when paper name is not found in the library</exception>
        public static explicit operator PaperSettings(string paperName)
        {
            // Get all available papers from both libraries
            var allPapers = new List<PaperSettings>();
            allPapers.AddRange(PaperSettingsLibrary.GetMetricPapers());
            allPapers.AddRange(PaperSettingsLibrary.GetImperialPapers());

            // Search for a paper with the matching name (case-insensitive)
            foreach (var paper in allPapers)
            {
                if (paper.Name.Equals(paperName, StringComparison.OrdinalIgnoreCase))
                {
                    return paper.Duplicate(); // Return a duplicate to avoid modifying the original
                }
            }

            // If we get here, no matching paper was found
            throw new ArgumentException($"Paper setting with name '{paperName}' not found in paper library.");
        }

        // Add these methods to your PaperSettings class

        public bool ValidateDimensions()
        {
            return Width > 0 && Height > 0;
        }

        public bool ValidateMargins()
        {
            // Check if any margin is negative
            if (MarginTop < 0 || MarginBottom < 0 || MarginLeft < 0 || MarginRight < 0)
                return false;

            // Check if margins exceed paper dimensions
            if (MarginLeft + MarginRight >= Width || MarginTop + MarginBottom >= Height)
                return false;

            return true;
        }

        public void EnsureValidDimensions()
        {
            if (Width <= 0)
                throw new ArgumentOutOfRangeException("Width", "Paper width must be greater than 0");

            if (Height <= 0)
                throw new ArgumentOutOfRangeException("Height", "Paper height must be greater than 0");
        }

        public void EnsureValidMargins()
        {
            if (MarginTop < 0)
                throw new ArgumentOutOfRangeException("MarginTop", "Top margin cannot be negative");

            if (MarginBottom < 0)
                throw new ArgumentOutOfRangeException("MarginBottom", "Bottom margin cannot be negative");

            if (MarginLeft < 0)
                throw new ArgumentOutOfRangeException("MarginLeft", "Left margin cannot be negative");

            if (MarginRight < 0)
                throw new ArgumentOutOfRangeException("MarginRight", "Right margin cannot be negative");

            if (MarginLeft + MarginRight >= Width)
                throw new ArgumentException("Horizontal margins exceed paper width");

            if (MarginTop + MarginBottom >= Height)
                throw new ArgumentException("Vertical margins exceed paper height");
        }

        public double ContentWidth => Width - MarginLeft - MarginRight;
        public double ContentHeight => Height - MarginTop - MarginBottom;
    }

    // Enums for the paper settings
    public enum PaperUnit
    {
        Inches,
        Millimeters,
        Centimeters
    }

    public enum PaperOrientation
    {
        Portrait,
        Landscape
    }

    /// <summary>
    /// Library of standard paper sizes
    /// </summary>
    public static class PaperSettingsLibrary
    {
        /// <summary>
        /// Get standard imperial paper sizes (in inches)
        /// </summary>
        public static List<PaperSettings> GetImperialPapers()
        {
            return new List<PaperSettings>
            {
                new PaperSettings("Letter", 8.5, 11, 0),
                new PaperSettings("Legal", 8.5, 14, 0),
                new PaperSettings("Tabloid/Ledger", 11, 17, 0),
                new PaperSettings("ANSI A", 8.5, 11, 0),
                new PaperSettings("ANSI B", 11, 17, 0),
                new PaperSettings("ANSI C", 17, 22, 0),
                new PaperSettings("ANSI D", 22, 34, 0),
                new PaperSettings("ANSI E", 34, 44, 0),
                new PaperSettings("Arch A", 9, 12, 0),
                new PaperSettings("Arch B", 12, 18, 0),
                new PaperSettings("Arch C", 18, 24, 0),
                new PaperSettings("Arch D", 24, 36, 0),
                new PaperSettings("Arch E", 36, 48, 0),
                // Common metric sizes converted to inches
                new PaperSettings("A4 (Metric)", 8.27, 11.69, 0),
                new PaperSettings("A3 (Metric)", 11.69, 16.54, 0),
                new PaperSettings("A2 (Metric)", 16.54, 23.39, 0),
                new PaperSettings("A1 (Metric)", 23.39, 33.11, 0)
            };
        }

        /// <summary>
        /// Get standard metric paper sizes (in mm)
        /// </summary>
        public static List<PaperSettings> GetMetricPapers()
        {
            return new List<PaperSettings>
            {
                new PaperSettings("A0", 841, 1189, PaperUnit.Millimeters),
                new PaperSettings("A1", 594, 841, PaperUnit.Millimeters),
                new PaperSettings("A2", 420, 594, PaperUnit.Millimeters),
                new PaperSettings("A3", 297, 420, PaperUnit.Millimeters),
                new PaperSettings("A4", 210, 297, PaperUnit.Millimeters),
                new PaperSettings("A5", 148, 210, PaperUnit.Millimeters),
                new PaperSettings("B0", 1000, 1414, PaperUnit.Millimeters),
                new PaperSettings("B1", 707, 1000, PaperUnit.Millimeters),
                new PaperSettings("B2", 500, 707, PaperUnit.Millimeters),
                new PaperSettings("B3", 353, 500, PaperUnit.Millimeters),
                new PaperSettings("B4", 250, 353, PaperUnit.Millimeters),
                new PaperSettings("B5", 176, 250, PaperUnit.Millimeters),
                // Common imperial sizes converted to mm
                new PaperSettings("Letter (US)", 216, 279, PaperUnit.Millimeters),
                new PaperSettings("Tabloid (US)", 279, 432, PaperUnit.Millimeters),
                new PaperSettings("ANSI B (US)", 279, 432, PaperUnit.Millimeters)
            };
        }
        /// <summary>
        /// Get paper sizes appropriate for the given unit system
        /// </summary>
        public static List<PaperSettings> GetPapersForUnitSystem(UnitSystem unitSystem)
        {
            string unitName = unitSystem.ToString().ToLowerInvariant();
            bool isMetric = unitName.EndsWith("meters");

            return isMetric ? GetMetricPapers() : GetImperialPapers();
        }

        public static List<PaperSettings> GetPapersForUnitSystem(PaperUnit unitSystem)
        {
            string unitName = unitSystem.ToString().ToLowerInvariant();
            bool isMetric = unitName.EndsWith("meters");

            return isMetric ? GetMetricPapers() : GetImperialPapers();
        }
    }
}