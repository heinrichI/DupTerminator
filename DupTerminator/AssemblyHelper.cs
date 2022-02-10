using System;
using System.Text;
using System.Reflection; //Assembly
using DupTerminator.Localize;

namespace DupTerminator
{
    class AssemblyHelper
    {
        public static string AssemblyTitle
        {
            get
            {
                // Get all Title attributes on this assembly
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                // If there is at least one Title attribute
                if (attributes.Length > 0)
                {
                    // Select the first one
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    // If it is not an empty string, return it
                    if (String.IsNullOrEmpty(titleAttribute.Title))
                        return titleAttribute.Title;
                }
                // If there was no Title attribute, or if the Title attribute was the empty string, return the .exe name
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        public static string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public static string AssemblyBuildDate
        {
            get
            {
                Assembly assem = Assembly.GetExecutingAssembly();
                AssemblyName assemName = assem.GetName();
                Version ver = assemName.Version;
                int build = Convert.ToInt32(ver.Build);
                int rev = Convert.ToInt32(ver.Revision);
                DateTime buildDate = new DateTime(2000, 1, 1);
                buildDate = buildDate.AddDays(build);
                buildDate = buildDate.AddSeconds(rev * 2);
                //System.Diagnostics.Debug.WriteLine("Build #: " + _build);
                //System.Diagnostics.Debug.WriteLine("Revision #: " + rev);
                //System.Diagnostics.Debug.WriteLine("This was built on: " + buildDate.ToLongDateString() + " at " + buildDate.ToLongTimeString());
                //return "Build on: " + buildDate.ToLongDateString() + " at " + buildDate.ToLongTimeString();
                //return "Build on: " + buildDate.ToUniversalTime();
                //return LanguageManager.GetString("BuildOn") + buildDate.ToUniversalTime();
                return String.Format(LanguageManager.GetString("BuildOn"), buildDate.ToShortDateString(), buildDate.ToLongTimeString());
                //return String.Format(LanguageManager.GetString("BuildOn"), buildDate.ToUniversalTime()," .");
            }
        }

        public static string AssemblyBuildString()
        {
            Assembly assem = Assembly.GetExecutingAssembly();
            AssemblyName assemName = assem.GetName();
            Version ver = assemName.Version;

            StringBuilder builder = new StringBuilder();
            builder.Append(ver.Build);
            builder.Append(".");
            builder.Append(ver.Revision);
            return builder.ToString();
        }
 

        public static string AssemblyDescription
        {
            get
            {
                // Get all Description attributes on this assembly
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                // If there aren't any Description attributes, return an empty string
                if (attributes.Length == 0)
                    return String.Empty;
                // If there is a Description attribute, return its value
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public static string AssemblyProduct
        {
            get
            {
                // Get all Product attributes on this assembly
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                // If there aren't any Product attributes, return an empty string
                if (attributes.Length == 0)
                    return String.Empty;
                // If there is a Product attribute, return its value
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public static string AssemblyCopyright
        {
            get
            {
                // Get all Copyright attributes on this assembly
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                // If there aren't any Copyright attributes, return an empty string
                if (attributes.Length == 0)
                    return String.Empty;
                // If there is a Copyright attribute, return its value
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public static string AssemblyCompany
        {
            get
            {
                // Get all Company attributes on this assembly
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                // If there aren't any Company attributes, return an empty string
                if (attributes.Length == 0)
                    return String.Empty;
                // If there is a Company attribute, return its value
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }
    }
}
