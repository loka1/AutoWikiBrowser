/*
AWB Profiles
Copyright (C) 2008 Sam Reed, Stephen Kennedy

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

using System.Collections.Generic;
using System.Windows.Forms;
using WikiFunctions.Encryption;
using Microsoft.Win32;

namespace WikiFunctions.Profiles
{
    public class AWBProfile
    {
        public AWBProfile(int pID, string pUsername, string pPassword, string pDefaultSettings, string pNotes,
            bool pUseForUpload)
        {
            id = pID;
            mUsername = pUsername;
            mPassword = pPassword;
            defaultsettings = pDefaultSettings;
            notes = pNotes;
            useforupload = pUseForUpload;
        }

        public AWBProfile() { }

        public int id;
        public string defaultsettings, notes;

        private string mUsername = "";
        private string mPassword = "";

        public bool useforupload;

        public string Username
        {
            get { return mUsername; }
            set { mUsername = value; }
        }

        public string Password
        {
            get { return mPassword; }
            set { mPassword = value; }
        }
    }

    public static class AWBProfiles
    {
        private static readonly EncryptionUtils EncryptionUtils = new EncryptionUtils("tnf47bgfdwlp9,.q",
            "oi frjweopi 4r390%^($%%^$HJKJNMHJGY 2`';'[#", "SH1ew yuhn gxe$?$%^y HNKLHWEQ JEW`b");
        private const string ProfileRegistryString = "Profiles\\";

        /// <summary>
        /// Gets all the Saved Profiles from the Registry
        /// </summary>
        /// <returns>List of Profiles</returns>
        public static List<AWBProfile> GetProfiles()
        {
            List<AWBProfile> profiles = new List<AWBProfile>();
            foreach (int id in GetProfileIDs())
            {
                profiles.Add(GetProfile(id));
            }
            return profiles;
        }

        /// <summary>
        /// Gets a Specified Profile from the Registry
        /// </summary>
        /// <param name="id">Profile ID to get</param>
        /// <returns>Specified Profile</returns>
        public static AWBProfile GetProfile(int id)
        {
            AWBProfile prof = new AWBProfile();

            prof.id = id;
            try { prof.Username = RegistryGetAndDecryptValue(id + "\\User", ""); }
            catch
            {
                if (MessageBox.Show("Profile corrupt. Would you like to delete this profile?", "Delete corrupt profile?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    DeleteProfile(id);
            }

            // one try...catch without a resume has the effect that all remaining code in the try block is skipped
            // WHY are we just ignoring these errors anyway? There should be a wrapper around Registry.GetValue perhaps?
            try { prof.Password = RegistryGetAndDecryptValue(id + "\\Pass", ""); }
            catch { prof.Password = ""; }
            finally
            {
                prof.defaultsettings = RegistryGetValue(id + "\\Settings", "");
                try { prof.useforupload = bool.Parse(RegistryGetValue(id + "\\UseForUpload", "")); }
                catch { prof.useforupload = false; }
                prof.notes = RegistryGetValue(id + "\\Notes", "");
            }
            return prof;
        }

        /// <summary>
        /// Return (or create and return) an AWBProfile for the account used for log uploading
        /// </summary>
        /// <returns>The Profile. Throw an error or return null if the user declines to create a profile?</returns>
        public static AWBProfile GetProfileForLogUploading(IWin32Window owner)
        {
            int idOfUploadAccount = GetIDOfUploadAccount();
            AWBProfile retval;
            AWBLogUploadProfilesForm profiles;

            if (idOfUploadAccount == -1)
            {
                if (MessageBox.Show("Please select or add a Profile to use for log uploading",
                    "Log uploading", MessageBoxButtons.OKCancel, MessageBoxIcon.Information)
                    == DialogResult.OK)
                {
                    profiles = new AWBLogUploadProfilesForm();
                    profiles.ShowDialog(owner);
                    retval = GetProfileForLogUploading(owner);
                }
                else
                    throw new System.Configuration.ConfigurationErrorsException("Log upload profile: User cancelled");
            }
            else
                retval = GetProfile(idOfUploadAccount);

            if (string.IsNullOrEmpty(retval.Password) && string.IsNullOrEmpty(TempPassword))
            {
                UserPassword password = new UserPassword();
                password.SetText = "Enter password for " + retval.Username;
                if (password.ShowDialog() == DialogResult.OK)
                {
                    retval.Password = TempPassword = password.GetPassword;
                }
            }
            else if (!string.IsNullOrEmpty(TempPassword))
                retval.Password = TempPassword;
            return retval;
        }

        /// <summary>
        /// Returns the ID of the account set to be used to upload logs
        /// </summary>
        /// <returns>-1 if no Upload Profile found</returns>
        public static int GetIDOfUploadAccount()
        {
            foreach (AWBProfile prof in GetProfiles())
                if (prof.useforupload)
                    return prof.id;
            return -1;
        }

        /// <summary>
        /// Sets all current accounts as not for upload, so the new account can be the upload account
        /// </summary>
        internal static void SetOtherAccountsAsNotForUpload()
        {
            try
            {
                ResetTempPassword();
                foreach (int id in GetProfileIDs())
                { RegistrySetValue(id, "UseForUpload", false.ToString()); }
            }
            catch { }
        }

        /// <summary>
        /// Gets the decrypted password of a specified profile
        /// </summary>
        /// <param name="id">Profile ID</param>
        /// <returns>Decrypted password</returns>
        public static string GetPassword(int id)
        { return RegistryGetAndDecryptValue(id + "\\Pass", ""); }

        /// <summary>
        /// Gets the decrypted username of a specified profile
        /// </summary>
        /// <param name="id">Profile ID</param>
        /// <returns>Decrypted password</returns>
        public static string GetUsername(int id)
        { return RegistryGetAndDecryptValue(id + "\\User", ""); }

        /// <summary>
        /// Set/Change the password of the specified profile
        /// </summary>
        /// <param name="id">Profile ID</param>
        /// <param name="password">Password</param>
        public static void SetPassword(int id, string password)
        { SetProfilePassword(id, EncryptionUtils.Encrypt(password)); }

        /// <summary>
        /// Sets the Profile Password in the Registry
        /// </summary>
        /// <param name="id">Profile ID</param>
        /// <param name="password">Password</param>
        private static void SetProfilePassword(int id, string password)
        {
            try { RegistrySetValue(id, "Pass", password); }
            catch { }
        }

        /// <summary>
        /// Writes a new or modified Profile to the registry
        /// </summary>
        /// <param name="profile">Profile Object of User</param>
        internal static void AddEditProfile(AWBProfile profile)
        {
            if (profile.id == -1)
                profile.id = GetFirstFreeID();

            RegistryKey Key = RegistryGetWritableKey(profile.id);

            try
            {
                Key.SetValue("User", EncryptionUtils.Encrypt(profile.Username));
                Key.SetValue("Pass", EncryptionUtils.Encrypt(profile.Password));
                Key.SetValue("Settings", profile.defaultsettings);
                Key.SetValue("UseForUpload", profile.useforupload);
                Key.SetValue("Notes", profile.notes);
            }
            catch { }
        }

        /// <summary>
        /// Gets/Returns the temporary password set by the user
        /// </summary>
        private static string TempPassword
        {
            get
            {
                try { return RegistryGetAndDecryptValue("TempPassword", ""); }
                catch { return ""; }
            }

            set
            {
                try
                {
                    RegistryKey key = RegistryUtils.GetWritableKey(ProfileRegistryString);
                    if (key != null) key.SetValue("TempPassword", EncryptionUtils.Encrypt(value));
                }
                catch { }
            }
        }

        /// <summary>
        /// Sets the temporary password to ""
        /// </summary>
        public static void ResetTempPassword()
        {
            TempPassword = "";
        }

        /// <summary>
        /// Deletes a specific profile from the registry
        /// </summary>
        /// <param name="id"></param>
        public static void DeleteProfile(int id)
        {
            try { RegistryUtils.DeleteSubKey(ProfileRegistryString + id); }
            catch { }
        }

        /// <summary>
        /// Gets a List of all the Profile IDs
        /// </summary>
        /// <returns>A list of all Profile IDs</returns>
        private static List<int> GetProfileIDs()
        {
            List<int> profileIds = new List<int>();
            try
            {
                foreach (string id in RegistryUtils.OpenSubKey(ProfileRegistryString).GetSubKeyNames())
                { profileIds.Add(int.Parse(id)); }
                return profileIds;
            }
            catch
            { return profileIds; }
        }

        /// <summary>
        /// Gets the ID number of the first free Profile Slot
        /// </summary>
        /// <returns>ID Number</returns>
        private static int GetFirstFreeID()
        {
            bool freeIdFound = false;
            List<int> ids = GetProfileIDs();
            int i = 1;

            do
            {
                if (!ids.Contains(i))
                    freeIdFound = true;
                else
                    i++;
            } while (!freeIdFound);

            return i;
        }

        // Wrapper around RegistryUtils
        private static string RegistryGetValue(string suffix, object defaultValue)
        { return RegistryUtils.GetValue(ProfileRegistryString + suffix, defaultValue); }

        private static string RegistryGetAndDecryptValue(string suffix, object defaultValue)
        { return EncryptionUtils.RegistryGetValueAndDecrypt(ProfileRegistryString + suffix, defaultValue); }

        private static void RegistrySetValue(int keyNameSuffix, string valueName, string value)
        { RegistryUtils.SetValue(ProfileRegistryString + keyNameSuffix, valueName, value); }

        private static Microsoft.Win32.RegistryKey RegistryGetWritableKey(int keyNameSuffix)
        { return RegistryUtils.GetWritableKey(ProfileRegistryString + keyNameSuffix); }

        public static void MigrateProfiles()
        {
            const string RegKey = "Software\\Wikipedia\\AutoWikiBrowser\\Profiles";
            try
            {
                foreach (string id in new Microsoft.VisualBasic.Devices.Computer().Registry.CurrentUser.OpenSubKey(RegKey).GetSubKeyNames())
                {
                    AWBProfile prof = new AWBProfile();

                    Microsoft.VisualBasic.Devices.Computer myComputer = new Microsoft.VisualBasic.Devices.Computer();

                    prof.id = int.Parse(id);
                    try
                    {
                        prof.Username = EncryptionUtils.Decrypt(myComputer.Registry.GetValue("HKEY_CURRENT_USER\\" + RegKey + "\\" + id, "User", "").ToString());

                        try { prof.Password = EncryptionUtils.Decrypt(myComputer.Registry.GetValue("HKEY_CURRENT_USER\\" + RegKey + "\\" + id, "Pass", "").ToString()); }
                        catch { prof.Password = ""; }

                        prof.defaultsettings = myComputer.Registry.GetValue("HKEY_CURRENT_USER\\" + RegKey + "\\" + id, "Settings", "").ToString();

                        try { prof.useforupload = bool.Parse(myComputer.Registry.GetValue("HKEY_CURRENT_USER\\" + RegKey + "\\" + id, "UseForUpload", "").ToString()); }
                        catch { prof.useforupload = false; }

                        prof.notes = myComputer.Registry.GetValue("HKEY_CURRENT_USER\\" + RegKey + "\\" + id, "Notes", "").ToString();
                    }
                    catch
                    { }

                    AddEditProfile(prof);
                }
            }
            catch
            { }
        }
    }

}
