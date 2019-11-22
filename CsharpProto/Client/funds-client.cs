//////////////////////////////////////////////////////////////////////
//
// File      : funds-client.cs
//
// Author    : Barry Kimelman
//
// Created   : November 11, 2019
//
// Purpose   : Client program to interface with the funds server
//
// Notes     : XmlDocument class will be used to parse the response from the server
//
//////////////////////////////////////////////////////////////////////

using System;
using System.Xml;
using System.Net;
using System.Web;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ConsoleApp1
{
	class Program
	{
		static bool debug_mode = false;
		static string url_part_1 = "http://localhost:88/cgi-bin/funds2-server.exe";
		static string username = "";
		static string password = "";
		static string function = "";
		static int status_code = 0;
		static string postData = "";
		static string error_message = "";
		static string error_details = "";
		static string responseString = "";
		static string[] user_input_fields = new string[] { "new_username" , "new_password" , "first_name" ,
							"last_name" , "email" , "phone" , "priv_level" , "balance1" ,
							"comment"};
		static string[] user_input_fields_titles = new string[] { "Username" , "Password" , "First Name" ,
							"Last Name" , "Email" , "Phone Number" , "Privilege Level" , "Initial Balance" ,
							"Comment"};
		static string help_text = @"
users     - display the list of registered users
admins    - display the list of registered administrative users
usernames - display the names of users matching a pattern
hist      - display the transaction history records
myhist    - display the transaction history records for my transactions
send      - send money to another user
adduser   - create a new user for the system
deluser   - mark a user as 'expired'
void      - mark a transaction as voided
balance   - display the sum of all the ballance values
info      - display general information about program operation
";
		static string help_info = @"
The users and hist commands display information differently depending on wether
or not the specified user is an administrator. If the specified user is an
administrator then all available information will be displayed else only
information relative to the specified user will be displayed.
";


//////////////////////////////////////////////////////////////////////
//
// Function  : debug_print
//
// Purpose   : Display message only if debug mode is active
//
// Inputs    : string message - the message to be displayed
//
// Output    : the requested message
//
// Returns   : nothing
//
// Example   : debug_print("The number of records is " + count);
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

		static public void debug_print(string message)
		{
			if ( debug_mode ) {
				Console.WriteLine(message);
			}
			return;
		} // end of debug_print

//////////////////////////////////////////////////////////////////////
//
// Function  : send_request
//
// Purpose   : send a request to the server
//
// Inputs    : string post_data - the POST data to be attached to the request
//
// Output    : appropriate messages
//
// Returns   : nothing
//
// Example   : send_request("The number of records is " + count);
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

		static public void send_request(string post_data)
		{
			var request = (HttpWebRequest)WebRequest.Create(url_part_1);

			var data = Encoding.ASCII.GetBytes(post_data);
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			request.ContentLength = data.Length;

			using (var stream = request.GetRequestStream())
			{
				stream.Write(data, 0, data.Length);
			}
 
			var response = (HttpWebResponse)request.GetResponse();

			responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
			int num_bytes = responseString.Length;
			debug_print("\nReceived " + num_bytes + " bytes from " + url_part_1 + "\nwith the postData : " + postData);
			debug_print("\n" + responseString);

// Parse the response sent by the server

			parse_response_status(responseString);

			return;
		} // end of send_request

//////////////////////////////////////////////////////////////////////
//
// Function  : parse_response_status
//
// Purpose   : Parse the status information in the server's response
//
// Inputs    : string data - response data received from server
//
// Output    : appropriate messages
//
// Returns   : nothing
//
// Example   : parse_response_status();
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

		static public void parse_response_status(string server_data)
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(server_data);
			XmlNodeList nodes2 = doc.DocumentElement.SelectNodes("/RESPONSE/STATUS");
			int count = 0;
			foreach (XmlNode node2 in nodes2)
			{
				count += 1;
				debug_print("\nProcess status node " + count);
				debug_print("status_code = " + node2.SelectSingleNode("status_code").InnerText);
				debug_print("error_message = " + node2.SelectSingleNode("error_message").InnerText);
				debug_print("error_details = " + node2.SelectSingleNode("error_details").InnerText);

				status_code = Convert.ToInt32(node2.SelectSingleNode("status_code").InnerText);
				error_message = node2.SelectSingleNode("error_message").InnerText;
				error_details = node2.SelectSingleNode("error_details").InnerText;
			} // foreach
			debug_print("\nnumber of status values = " + count);
			Console.WriteLine("\nStatus Code = " + status_code);
			Console.WriteLine("Status Message : " + error_message);
			Console.WriteLine("Message Details : " + error_details);

			return;
		} // end of parse_response_status

//////////////////////////////////////////////////////////////////////
//
// Function  : cmd_users
//
// Purpose   : Process the requested users command
//
// Inputs    : (none)
//
// Output    : appropriate messages
//
// Returns   : nothing
//
// Example   : cmd_users();
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

		static public void cmd_users()
		{
			postData = "username=" + username + "&password=" + password + "&function=users";
			send_request(postData);

			XmlDocument doc = new XmlDocument();
			doc.LoadXml(responseString);

			XmlNodeList nodes = doc.DocumentElement.SelectNodes("/RESPONSE/USER");
			int count = 0;
			int total_balance = 0;
			foreach (XmlNode node in nodes)
			{
				count += 1;
				Console.Write("\n");
				Console.Write("Username : " + node.SelectSingleNode("username").InnerText);
				Console.Write(" , Id : " + node.SelectSingleNode("id").InnerText);
				Console.Write(" , Name : " + node.SelectSingleNode("first_name").InnerText + " " +
								node.SelectSingleNode("last_name").InnerText);
				Console.Write(" , Password : " + node.SelectSingleNode("password").InnerText);
				Console.Write("\n");
				Console.Write("mod_date : " + node.SelectSingleNode("mod_date").InnerText);
				Console.Write(" , email : " + node.SelectSingleNode("email").InnerText);
				Console.Write(" , Privilege Level : " + node.SelectSingleNode("priv_level").InnerText);
				Console.Write("\n");
				Console.Write("Phone : " + node.SelectSingleNode("phone").InnerText);
				Console.Write(" , Initial Balance : " + node.SelectSingleNode("balance1").InnerText);
				Console.Write(" , Current Balance : " + node.SelectSingleNode("balance").InnerText);
				Console.Write("\n");
				Console.Write("Status : " + node.SelectSingleNode("status").InnerText);
				Console.Write(" , Comment : " + node.SelectSingleNode("comment").InnerText);
				Console.Write("\n");

				total_balance += Convert.ToInt32(node.SelectSingleNode("balance").InnerText);
			} // foreach

			System.Console.WriteLine("\nTotal number of users : " + count);
			double value;
			double d_count = count;
			value = total_balance;
			value = value / d_count;
			Console.WriteLine("\nTotal balance from listed users = " + value.ToString("C", CultureInfo.CurrentCulture));

			return;
		} // end of cmd_users

//////////////////////////////////////////////////////////////////////
//
// Function  : cmd_admins
//
// Purpose   : Process the requested admins command
//
// Inputs    : (none)
//
// Output    : appropriate messages
//
// Returns   : nothing
//
// Example   : cmd_admins();
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

		static public void cmd_admins()
		{
			postData = "username=" + username + "&password=" + password + "&function=admins";
			send_request(postData);

			XmlDocument doc = new XmlDocument();
			doc.LoadXml(responseString);

			XmlNodeList nodes = doc.DocumentElement.SelectNodes("/RESPONSE/USER");
			int count = 0;
			int num_users = 0;
			foreach (XmlNode node in nodes)
			{
				num_users += 1;
				int priv = Convert.ToInt32(node.SelectSingleNode("priv_level").InnerText);
				if ( priv == 0 ) {
					count += 1;
					Console.Write("\n");
					Console.Write("Username : " + node.SelectSingleNode("username").InnerText);
					Console.Write(" , Name : " + node.SelectSingleNode("first_name").InnerText + " " +
								node.SelectSingleNode("last_name").InnerText);
					Console.Write("\n");
					Console.Write("email : " + node.SelectSingleNode("email").InnerText);
					Console.Write(" , Phone : " + node.SelectSingleNode("phone").InnerText);
					Console.Write("\n");
				} // IF an admin user
			} // foreach
			System.Console.WriteLine("\nNumber of users = " + num_users + " , Number of administrators : " + count);

			return;
		} // end of cmd_admins

//////////////////////////////////////////////////////////////////////
//
// Function  : cmd_usernames
//
// Purpose   : Process the requested usernames command
//
// Inputs    : (none)
//
// Output    : appropriate messages
//
// Returns   : nothing
//
// Example   : cmd_usernames();
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

		static public void cmd_usernames()
		{
			postData = "username=" + username + "&password=" + password + "&function=users";
			send_request(postData);

			Console.Write("\nEnter username pattern ==> ");
			string value = Console.ReadLine();
			value = value.ToString().TrimEnd('\r', '\n');

			XmlDocument doc = new XmlDocument();
			doc.LoadXml(responseString);

			XmlNodeList nodes = doc.DocumentElement.SelectNodes("/RESPONSE/USER");
			int count = 0;
			int matched = 0;
			foreach (XmlNode node in nodes)
			{
				count += 1;
				string uname = node.SelectSingleNode("username").InnerText;
				if (Regex.IsMatch(uname, value, RegexOptions.IgnoreCase)) {
					matched += 1;
					Console.WriteLine(uname);
				}

			} // foreach

			Console.WriteLine("\nTotal number of users : " + count);
			Console.WriteLine("Number of usernames matched by '" + value + "' is : " + matched);

			return;
		} // end of cmd_usernames

//////////////////////////////////////////////////////////////////////
//
// Function  : cmd_hist
//
// Purpose   : Process the requested hist command
//
// Inputs    : string func - "hist" or "myhist"
//
// Output    : appropriate messages
//
// Returns   : nothing
//
// Example   : cmd_hist("hist");
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

		static public void cmd_hist(string func)
		{
			int bal;
			double value;

			postData = "username=" + username + "&password=" + password + "&function=" + func;
			send_request(postData);
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(responseString);

			XmlNodeList nodes = doc.DocumentElement.SelectNodes("/RESPONSE/TRAN");
			int count = 0;
			int num_sent = 0;
			int num_received = 0;
			foreach (XmlNode node in nodes)
			{
				count += 1;
				Console.Write("\n");
				Console.Write("Transaction Id : " + node.SelectSingleNode("id").InnerText);
				Console.Write(" , Date : " + node.SelectSingleNode("mod_date").InnerText);
				Console.Write("\n");
				Console.Write("Operation : " + node.SelectSingleNode("operation").InnerText);
				bal = Convert.ToInt32(node.SelectSingleNode("amount").InnerText);
				value = bal;
				value = value / 100.0;
				
				Console.Write(" , Amount : " + value.ToString("C", CultureInfo.CurrentCulture));
				Console.Write(" , Status : " + node.SelectSingleNode("status").InnerText);
				Console.Write("\n");
				Console.Write("Sender : Username = " + node.SelectSingleNode("name1").InnerText);
				bal = Convert.ToInt32(node.SelectSingleNode("user1_balance").InnerText);
				value = bal;
				value = value / 100.0;

				Console.Write(" , Balance after operation = " + value.ToString("C", CultureInfo.CurrentCulture));
				Console.Write("\n");
				Console.Write("Recipient : Username = " + node.SelectSingleNode("name2").InnerText);
				bal = Convert.ToInt32(node.SelectSingleNode("user2_balance").InnerText);
				value = bal;
				value = value / 100.0;

				Console.Write(" , Balance after operation = " + value.ToString("C", CultureInfo.CurrentCulture));
				Console.Write("\n");
				if ( username == node.SelectSingleNode("name1").InnerText )
					num_sent += 1;
				if ( username == node.SelectSingleNode("name2").InnerText )
					num_received += 1;
			} // foreach

			System.Console.WriteLine("\nTotal number of transactions listed : " + count);
			Console.WriteLine("For " + username + " : Number sent = " + num_sent + " , Number received = " + num_received);

			return;
		} // end of cmd_hist

//////////////////////////////////////////////////////////////////////
//
// Function  : cmd_adduser
//
// Purpose   : Process the requested adduser command
//
// Inputs    : (none)
//
// Output    : appropriate messages
//
// Returns   : nothing
//
// Example   : cmd_adduser();
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

		static public void cmd_adduser()
		{
			int len , num_fields;
			string value;
			postData = "username=" + username + "&password=" + password + "&function=adduser";
			num_fields = user_input_fields.Length;
			for ( int index = 0 ; index < num_fields ; ++index ) {
				do {
					Console.Write("\nEnter value for " + user_input_fields_titles[index] + " ==> ");
					value = Console.ReadLine();
					value = value.ToString().TrimEnd('\r', '\n');
					Console.WriteLine("You entered [" + value + "]");
					len = value.Length;
				} while ( len == 0);
				postData += "&" + user_input_fields[index] + "=" + value;
			} // FOR
			Console.WriteLine("The postData to be sent is\n" + postData + "\n");
			send_request(postData);

			XmlDocument doc = new XmlDocument();
			doc.LoadXml(responseString);

			return;
		} // cmd_adduser

//////////////////////////////////////////////////////////////////////
//
// Function  : cmd_deluser
//
// Purpose   : Process the requested deluser command
//
// Inputs    : (none)
//
// Output    : appropriate messages
//
// Returns   : nothing
//
// Example   : cmd_deluser();
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

		static public void cmd_deluser()
		{
			string value;
			int len;

			do {
				Console.Write("\nEnter name of existing user ==> ");
				value = Console.ReadLine();
				value = value.ToString().TrimEnd('\r', '\n');
				len = value.Length;
			} while ( len == 0);

			postData = "username=" + username + "&password=" + password + "&function=deluser" +
							"&olduser=" + value;
			send_request(postData);

			XmlDocument doc = new XmlDocument();
			doc.LoadXml(responseString);

			return;
		} // cmd_deluser

//////////////////////////////////////////////////////////////////////
//
// Function  : cmd_void
//
// Purpose   : Process the requested void command
//
// Inputs    : (none)
//
// Output    : appropriate messages
//
// Returns   : nothing
//
// Example   : cmd_void();
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

		static public void cmd_void()
		{
			string value;
			int len;

			do {
				Console.Write("\nEnter id of transaction to be voided ==> ");
				value = Console.ReadLine();
				value = value.ToString().TrimEnd('\r', '\n');
				len = value.Length;
			} while ( len == 0);

			postData = "username=" + username + "&password=" + password + "&function=void&tran_id=" + value;
			send_request(postData);

			return;
		} // cmd_void

//////////////////////////////////////////////////////////////////////
//
// Function  : cmd_balance
//
// Purpose   : Process the requested balance command
//
// Inputs    : (none)
//
// Output    : appropriate messages
//
// Returns   : nothing
//
// Example   : cmd_balance();
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

		static public void cmd_balance()
		{
			postData = "username=" + username + "&password=" + password + "&function=users";
			send_request(postData);

			XmlDocument doc = new XmlDocument();
			doc.LoadXml(responseString);

			XmlNodeList nodes = doc.DocumentElement.SelectNodes("/RESPONSE/USER");
			int count = 0;
			int total_balance = 0;
			foreach (XmlNode node in nodes)
			{
				count += 1;
				total_balance += Convert.ToInt32(node.SelectSingleNode("balance").InnerText);
			} // foreach

			System.Console.WriteLine("\nTotal number of users : " + count);
			double value;
			double d_count = count;
			value = total_balance;
			value = value / d_count;
			Console.WriteLine("\nTotal balance from all users = " + value.ToString("C", CultureInfo.CurrentCulture));

			return;
		} // end of cmd_balance

//////////////////////////////////////////////////////////////////////
//
// Function  : cmd_info
//
// Purpose   : Process the requested info command
//
// Inputs    : (none)
//
// Output    : appropriate messages
//
// Returns   : nothing
//
// Example   : cmd_info();
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

		static public void cmd_info()
		{
			Console.WriteLine(help_info);

			return;
		} // end of cmd_info

//////////////////////////////////////////////////////////////////////
//
// Function  : cmd_send
//
// Purpose   : Process the requested send command
//
// Inputs    : string recipient - name of user to receive funds
//             string amount - amount of money to be sent in pennies
//
// Output    : appropriate messages
//
// Returns   : nothing
//
// Example   : cmd_send("fred",1000);
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

		static public void cmd_send(string recipient, string amount)
		{
			postData = "username=" + username + "&password=" + password +
							"&function=send&username2=" + recipient + "&amount=" + amount;
			send_request(postData);

			return;
		} // end of cmd_send

//////////////////////////////////////////////////////////////////////
//
// Function  : Main
//
// Purpose   : main program
//
// Inputs    : (none)
//
// Output    : appropriate messagess
//
// Returns   : zero
//
// Example   : funds-client.exe username password function
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

		static void Main(string[] args)
		{
			int num_args = args.Length;
			if ( num_args < 3 ) {
				Console.WriteLine("Usage : funds-client.exe username password function [optional parameters]");
				Environment.Exit(0);
			}
			username = args[0];
			password = args[1];
			function = args[2];
			switch ( function )
			{
				case "users":
					cmd_users();
					break;
				case "admins":
					cmd_admins();
					break;
				case "usernames":
					cmd_usernames();
					break;
				case "hist":
					cmd_hist("hist");
					break;
				case "myhist":
					cmd_hist("myhist");
					break;
				case "send":
					if ( num_args < 5 ) {
						Console.WriteLine("Usage : funds-client.exe username password send username2 amount");
					}
					else
						cmd_send(args[3],args[4]);
					break;
				case "adduser":
					cmd_adduser();
					break;
				case "deluser":
					cmd_deluser();
					break;
				case "void":
					cmd_void();
					break;
				case "balance":
					cmd_balance();
					break;
				case "help":
					Console.WriteLine(help_text);
					break;
				case "info":
					cmd_info();
					break;
				default:
					Console.WriteLine(function + " is not a valid function");
					Environment.Exit(0);
					break;
			} // switch
		} // end of Main
	} // end of Program

} // end of ConsoleApp1
