/*

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA

 */

using System;
using System.Reflection;

namespace WikiFunctions
{
    /// <summary>
    /// Holds some deepest-level things to be initialised prior to most other static classes,
    /// including Variables
    /// </summary>
    public static class Globals
    {
        static Globals()
        {
            try
            {
                System.Reflection.Assembly.Load("System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
                systemCore3500Avail = true;
            }
            catch
            {
            }
        }

        /// <summary>
        /// Whether we are running under Windows
        /// </summary>
        public static bool RunningOnWindows
        { get { return Windows; } }

        private static readonly bool Windows = Environment.OSVersion.VersionString.Contains("Windows");

        private static readonly bool Mono = Type.GetType("Mono.Runtime") != null;
        /// <summary>
        /// Returns whether we are using the Mono Runtime
        /// </summary>
        public static bool UsingMono
        { get { return Mono; } }

        /// <summary>
        /// Returns the WikiFunctions assembly version
        /// </summary>
        public static Version WikiFunctionsVersion
        {
            get { return Assembly.GetAssembly(typeof(Variables)).GetName().Version; }
        }

        private static readonly bool systemCore3500Avail = false;

        /// <summary>
        /// Returns whether System.Core, Version=3.5.0.0 is available
        /// So whether HashSets can be used (should be available in all .NET 2 but seems to rely on a cetain service pack level)
        /// </summary>
        public static bool SystemCore3500Available
        {
            get { return systemCore3500Avail; }
        }

        private static  bool? mSHTMLAvailable = null;

        /// <summary>
        /// Returns whether Microsoft.mshtml, Version=7.0.3300.0 is loaded
        /// So whether IHTMLDocument2 object in AWBWebBrowser can be used
        /// </summary>
        public static bool MSHTMLAvailable
        {
            get {
                if(mSHTMLAvailable == null)
                {
                    mSHTMLAvailable = false;
                    foreach(var a in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if(a.FullName.Equals("Microsoft.mshtml, Version=7.0.3300.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"))
                        {
                            mSHTMLAvailable = true;
                            break;
                        }
                    }
                }

                return (bool)mSHTMLAvailable;
            }
        }

        #region Unit tests support
        /// <summary>
        /// Set this to true in unit tests, to disable checkpage loading and other slow stuff.
        /// This disables some functions, however.
        /// </summary>
        public static bool UnitTestMode;

        /// <summary>
        /// 
        /// </summary>
        public static int UnitTestIntValue;

        /// <summary>
        /// 
        /// </summary>
        public static bool UnitTestBoolValue;
        #endregion
    }
}
