﻿/*
 * Copyright (C) 2010 Colin Mackie.
 * This software is distributed under the terms of the GNU General Public License.
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace WindowsAuthenticator
{
	/// <summary>
	/// Class holding configuration information and the current authenticator
	/// </summary>
	public class WinAuthConig
	{
		/// <summary>
		/// Name of application folder in "Application Data" for saving config
		/// </summary>
		public const string APPLICATION_FOLDER = "WinAuth";

		/// <summary>
		/// Default name of config file
		/// </summary>
		public const string CONFIG_FILENAME = "authenticator.xml";

		/// <summary>
		/// Application ID used to build the device ID - must not change
		/// </summary>
		public const string DEVICE_APPLICATION_ID = "WINDOWSAUTHENTICATOR";

		/// <summary>
		/// Get/set the current authenticator
		/// </summary>
		public Authenticator CurrentAuthenticator { get; set; }

		/// <summary>
		/// Get/set flag to auto refresh the code
		/// </summary>
		public bool AutoRefresh { get; set; }

		/// <summary>
		/// Get/set the flag to show the serial number
		/// </summary>
		public bool ShowSerial{ get; set; }

		/// <summary>
		/// Load the authenticator into the config from the default config file
		/// </summary>
		/// <returns>true if file loaded successfully</returns>
		public bool LoadAuthenticator()
		{
			// get the deault file
			string authfile = ConfigFile;
			// not exists then return false
			if (File.Exists(authfile) == false)
			{
				return false;
			}

			// get the password as the device ID
			string password = WinAPI.GetDeviceID(DEVICE_APPLICATION_ID);

			// read the authenticator file
			using (FileStream fs = new FileStream(authfile, FileMode.Open))
			{
				// create and set a loaded authenticator
				Authenticator auth = new Authenticator();
				auth.Load(fs, Authenticator.FileFormat.WinAuth, password);
				CurrentAuthenticator = auth;
			}

			// resave so we change the "user" password to "explicit" using the generic key for the device
			if (CurrentAuthenticator.PasswordType == Authenticator.PasswordTypes.User)
			{
				SaveAuthenticator();
			}

			return true;
		}

		/// <summary>
		/// Save the current authenticator into the config file
		/// </summary>
		public void SaveAuthenticator()
		{
			// if no authenticaotr just return
			if (CurrentAuthenticator == null)
			{
				return;
			}

			// get the file and make sure the folders are created
			string authfile = ConfigFile;
			Directory.CreateDirectory(Path.GetDirectoryName(authfile));

			// save the authenticator
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.CloseOutput = true;
			settings.Indent = true;
			using (XmlWriter xw = XmlWriter.Create(new FileStream(authfile, FileMode.Create), settings))
			{
				// make sure we are encrpyted using the deviceID
				string password = WinAPI.GetDeviceID(DEVICE_APPLICATION_ID);
				if (password != null)
				{
					CurrentAuthenticator.Password = password;
					CurrentAuthenticator.PasswordType = Authenticator.PasswordTypes.Explicit;
				}
				else
				{
					// if we couldn't get a device key, use none so at least we can guarentee the key
					CurrentAuthenticator.PasswordType = Authenticator.PasswordTypes.None;
				}

				// save the data
				CurrentAuthenticator.WriteXmlString(xw);
			}
		}

		/// <summary>
		/// Get the path of the default config file
		/// </summary>
		public static string ConfigFile
		{
			get
			{
				string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), APPLICATION_FOLDER);
				return Path.Combine(path, CONFIG_FILENAME);
			}
		}

	}
}