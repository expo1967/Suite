//////////////////////////////////////////////////////////////////////
//
// File      : funds2-server.cs
//
// Author    : Barry Kimelman
//
// Created   : November 10, 2019
//
// Purpose   : CGI/MySQL Test
//
// Notes     : modification of funds.cs (encapsulate status info in a <STATUS> node)
//
//////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;

namespace ConsoleApplication1 {
	class ConsoleApplication1 {
		static string input_data = "";
		static int DataLength;
		static int debug_mode = 0;
		static int max_post_length = 2048;
		static char[] SplitChars1 = new char[]{'&'} ;
		static char[] SplitChars2 = new char[]{'='} ;
		static int num_vars;
		static string[] input_fields;
		static Dictionary<string, string> data_fields = new Dictionary<string, string>();
		static SmartUser current_user;
		static Dictionary<string, bool> cmd_priv_level = new Dictionary<string, bool>();
		static Dictionary<string, string> numeric_fields = new Dictionary<string, string>();
		static string[] new_user_input_fields = new string[] { "new_username" , "new_password" , "first_name" ,
							"last_name" , "email" , "phone" , "priv_level" , "balance1" ,
							"comment"};
		static int num_new_user_input_fields = new_user_input_fields.Length;
		static string connect_string = @"server=localhost;userid=root;
				password=archer-nx01;database=qwlc";

		static string response_data; // generated response data

		static Dictionary<string, int> user_index = new Dictionary<string, int>();
		static Dictionary<int, string> uid_to_name = new Dictionary<int, string>();
		static List<SmartUser> UsersList = new List<SmartUser>();
		static Dictionary<int, int> trans_index = new Dictionary<int, int>();
		static List<SmartTrans> TransList = new List<SmartTrans>();

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
			if ( debug_mode != 0 ) {
				Console.WriteLine("<DEBUG>" + message + "</DEBUG>");
			}
			return;
		} // end of debug_print

//////////////////////////////////////////////////////////////////////
//
// Function  : send_response
//
// Purpose   : Send a response back to the client
//
// Inputs    : int - status code for response
//             string - value for error_message
//             string - value for error_details
//
// Output    : (none)
//
// Returns   : nothing
//
// Example   : send_response(1,"Can't read users table",ex.ToString());
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

		static public void send_response(int status, string err_msg, string err_details)
		{
			Console.WriteLine("<RESPONSE>");
			Console.WriteLine("<STATUS>");
			Console.WriteLine("<status_code>" + status + "</status_code>");
			Console.WriteLine("<error_message>" + err_msg + "</error_message>");
			if ( err_details.Length == 0 )
				err_details = ".";
			Console.WriteLine("<error_details>" + err_details + "</error_details>");
			Console.WriteLine("</STATUS>");
			if ( response_data.Length > 0 ) {
				Console.WriteLine(response_data);
			}
			Console.WriteLine("</RESPONSE>");
			Environment.Exit(0);
		} // end of send_response

//////////////////////////////////////////////////////////////////////
//
// Function  : read_smart_users_table
//
// Purpose   : read in the smart_users table
//
// Inputs    : (none)
//
// Output    : appropriate messages
//
// Returns   : nothing
//
// Example   : read_smart_users_table();
//
// Notes     : Program execution is terminated upon database error
//
//////////////////////////////////////////////////////////////////////

		static public void read_smart_users_table()
		{
			MySqlConnection conn = null;
			MySqlDataReader rdr = null;

			try 
			{
				conn = new MySqlConnection(connect_string);
				conn.Open();
        
				string query =@"SELECT id,mod_date,username,
							aes_decrypt(password,'pizza') password,first_name,
					   last_name,email,phone,priv_level,balance1,balance,status,
					   ifnull(comment,'--') comment
					FROM smart_users order by username";

				MySqlCommand cmd = new MySqlCommand(query, conn);
				rdr = cmd.ExecuteReader();

				int num_users = 0;
				while (rdr.Read()) 
				{
					num_users += 1;
					int id = rdr.GetInt32(0);
					string mod_date = rdr.GetString(1);
					string u_name = rdr.GetString(2);
					string password = rdr.GetString(3);
					string first_name = rdr.GetString(4);
					string last_name = rdr.GetString(5);
					string email = rdr.GetString(6);
					string phone = rdr.GetString(7);
					int priv_level = rdr.GetInt32(8);
					int balance1 = rdr.GetInt32(9);
					int balance = rdr.GetInt32(10);
					string status = rdr.GetString(11);
					string comment = rdr.GetString(12);
					SmartUser new_u = new SmartUser() {
						id = id , mod_date = mod_date , username = u_name ,
						password = password , first_name = first_name ,
						last_name = last_name , email = email ,
						phone = phone , priv_level = priv_level ,
						balance1 = balance1 , balance = balance ,
						status = status , comment = comment
					};
					UsersList.Insert(num_users-1,new_u);
					user_index.Add(u_name,num_users-1);
					uid_to_name.Add(id,u_name);
				} // WHILE over smart_user records

			} catch (MySqlException ex) 
			{
				send_response(1,"Can't read users table",ex.ToString());
			} finally 
			{
				if (rdr != null) 
				{
					rdr.Close();
				}

				if (conn != null) 
				{
					conn.Close();
				}
			} // finally

			return;
		} // end of read_smart_users_table

//////////////////////////////////////////////////////////////////////
//
// Function  : read_smart_users_history_table
//
// Purpose   : read in the contents of the smart_users_history table
//
// Inputs    : (none)
//
// Output    : appropriate messages
//
// Returns   : nothing
//
// Example   : read_smart_users_history_table();
//
// Notes     : Program execution is terminated upon database error
//
//////////////////////////////////////////////////////////////////////

		static public void read_smart_users_history_table()
		{
			MySqlConnection conn = null;
			MySqlDataReader rdr = null;

			try 
			{
				conn = new MySqlConnection(connect_string);
				conn.Open();
			
				string query =@"SELECT id,mod_date,user1,user1_balance,user2,user2_balance,operation,status,amount,
(select username from smart_users where id = h.user1) name1,
(select username from smart_users where id = h.user2) name2
FROM smart_users_history h
ORDER BY mod_date";

				MySqlCommand cmd = new MySqlCommand(query, conn);
				rdr = cmd.ExecuteReader();

				int count = 0;
				while (rdr.Read()) 
				{
					count += 1;
					int id = rdr.GetInt32(0);
					string mod_date = rdr.GetString(1);
					int user1 = rdr.GetInt32(2);
					int user1_balance = rdr.GetInt32(3);
					int user2 = rdr.GetInt32(4);
					int user2_balance = rdr.GetInt32(5);
					string operation = rdr.GetString(6);
					string status = rdr.GetString(7);
					int amount = rdr.GetInt32(8);
					string name1 = rdr.GetString(9);
					string name2 = rdr.GetString(10);

					SmartTrans new_t = new SmartTrans() {
							id = id , mod_date = mod_date ,
							user1 = user1 , user1_balance = user1_balance ,
							user2 = user2 , user2_balance = user2_balance ,
							operation = operation , status = status ,
							amount = amount ,name1 = name1 , name2 = name2
					};
					TransList.Insert(count-1,new_t);
					trans_index.Add(id,count-1);
				} // WHILE

			} catch (MySqlException ex) 
			{
				Console.WriteLine("Error: {0}",  ex.ToString());

			} finally 
			{
				if (rdr != null) 
				{
					rdr.Close();
				}

				if (conn != null) 
				{
					conn.Close();
				}

			} // finally

			return;
		} // end of read_smart_users_history_table

//////////////////////////////////////////////////////////////////////
//
// Function  : parse_input_fields
//
// Purpose   : Parse the parameters passed to this CGI script
//
// Inputs    : (none)
//
// Output    : debug mode messages
//
// Returns   : nothing
//
// Example   : parse_input_fields();
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

		static public void parse_input_fields()
		{
			if (System.Environment.GetEnvironmentVariable("REQUEST_METHOD").Equals("POST")) {
				DataLength = Convert.ToInt32(System.Environment.GetEnvironmentVariable("CONTENT_LENGTH"));
				debug_print("DataLength = " + DataLength);
				if (DataLength > max_post_length) DataLength = max_post_length;  // Max length for POST data
					for (int i = 0; i < DataLength; i++)
						input_data += Convert.ToChar(Console.Read()).ToString();
					// debug_print("<br/>Post Data length: " + DataLength.ToString() + " Post data: " + input_data);
			}
			else {
				input_data = System.Environment.GetEnvironmentVariable("QUERY_STRING");
				if ( input_data == null ) {
					DataLength = 0;
					input_data = "";
					debug_print("<br/>The GET Query String was not specified");
				}
				else {
					DataLength = input_data.Length;
					debug_print("<br/>The GET Query String (env: QUERY_STRING): " + input_data);
				}
			}

			debug_print("<H4>Parse the " + DataLength + " bytes of input_data</H4>");
			if ( DataLength == 0 ) {
				num_vars = 0;
				debug_print("<br><h3>number of input fields is zero</h3>");
			}
			else {
				input_fields = input_data.Split(SplitChars1) ;
				num_vars = input_fields.Length;
				debug_print("<br><BR>number of input fields = " + num_vars);
				for ( int var_index = 0 ; var_index < num_vars ; ++var_index ) {
					string[] parts = input_fields[var_index].Split(SplitChars2) ;
					int num_parts = parts.Length;
					debug_print("<h4>name = " + parts[0] + " , value = " + parts[1] +"</H4>");
					data_fields.Add(parts[0], parts[1]);
				}
			}

			return;
		} // end of parse_input_fields

//////////////////////////////////////////////////////////////////////
//
// Function  : validate_user_info
//
// Purpose   : Validate user information
//
// Inputs    : (none)
//
// Output    : appropriate info
//
// Returns   : nothing
//
// Example   : validate_user_info();
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

		static public void validate_user_info()
		{
			if ( !data_fields.ContainsKey("username") ) {
				send_response(1,"username was not specified","");
			}
			string user_name = data_fields["username"].ToLower();
			if ( !data_fields.ContainsKey("password") ) {
				send_response(1,"password was not specified","");
			}
			string pass = data_fields["password"];
			if ( !user_index.ContainsKey(user_name) ) {
				send_response(1,user_name + " is not a valid username","");
			}
			int u_index = user_index[user_name];
			current_user = UsersList[u_index];
			if ( current_user.password != pass ) {
				send_response(1,"Invalid password specified for " + user_name,"");
			}
			if ( current_user.status != "active" ) {
				send_response(1,user_name + " is no longer an active user","");
			}

			return;
		} // end of validate_user_info

//////////////////////////////////////////////////////////////////////
//
// Function  : cmd_users
//
// Purpose   : process a "users" command
//
// Inputs    : string func - "users" or "admins"
//
// Output    : appropriate info
//
// Returns   : nothing
//
// Example   : cmd_users();
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

		static public void cmd_users(string func)
		{
			for ( int index = 0 ; index < UsersList.Count ; ++index ) {
				if ( func == "admins" || current_user.priv_level == 0 || current_user.username == UsersList[index].username ) {
					response_data += "<USER>\n";
					response_data += "<id>" + UsersList[index].id + "</id>\n";
					response_data += "<username>" + UsersList[index].username + "</username>\n";
					response_data += "<mod_date>" + UsersList[index].mod_date + "</mod_date>\n";
					response_data += "<password>" + UsersList[index].password + "</password>\n";
					response_data += "<first_name>" + UsersList[index].first_name + "</first_name>\n";
					response_data += "<last_name>" + UsersList[index].last_name + "</last_name>\n";
					response_data += "<email>" + UsersList[index].email + "</email>\n";
					response_data += "<phone>" + UsersList[index].phone + "</phone>\n";
					response_data += "<priv_level>" + UsersList[index].priv_level + "</priv_level>\n";
					response_data += "<balance1>" + UsersList[index].balance1 + "</balance1>\n";
					response_data += "<balance>" + UsersList[index].balance + "</balance>\n";
					response_data += "<status>" + UsersList[index].status + "</status>\n";
					response_data += "<comment>" + UsersList[index].comment + "</comment>\n";
					response_data += "</USER>\n";
				}
			}
			send_response(0,"SUCCESS",func + " request was successfull");

			return;
		} // end of cmd_users

//////////////////////////////////////////////////////////////////////
//
// Function  : cmd_hist
//
// Purpose   : process a "hist" or "myhist" command
//
// Inputs    : string func - "hist" or "myhist"
//
// Output    : appropriate info
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

			if ( debug_mode != 0 ) {
				response_data += "<NAMES>\n";
				uid_to_name.ToList().ForEach(x => response_data += "<UID_NAME>id = " + x.Key + " , value = " + x.Value + "</UID_NAME>");
				response_data += "</NAMES>\n";
			}

			for ( int index = 0 ; index < TransList.Count ; ++index ) {
				if ( current_user.priv_level == 0 || current_user.id == TransList[index].user1 ||
								current_user.id == TransList[index].user2 ) {
					if ( func == "myhist" && current_user.id != TransList[index].user1 &&
								current_user.id != TransList[index].user2 )
						continue;
					response_data += "<TRAN>\n";
					response_data += "<id>" + TransList[index].id + "</id>\n";
					response_data += "<mod_date>" + TransList[index].mod_date + "</mod_date>\n";
					response_data += "<user1>" + TransList[index].user1 + "</user1>\n";
					response_data += "<user1_balance>" + TransList[index].user1_balance + "</user1_balance>\n";
					response_data += "<user2>" + TransList[index].user2 + "</user2>\n";
					response_data += "<user2_balance>" + TransList[index].user2_balance + "</user2_balance>\n";
					response_data += "<operation>" + TransList[index].operation + "</operation>\n";
					response_data += "<status>" + TransList[index].status + "</status>\n";
					response_data += "<amount>" + TransList[index].amount + "</amount>\n";
					response_data += "<name1>" + TransList[index].name1 + "</name1>";
					response_data += "<name2>" + TransList[index].name2 + "</name2>";
					response_data += "</TRAN>\n";
				}
			}
			send_response(0,"SUCCESS","hist request was successfull");

			return;
		} // end of cmd_hist

//////////////////////////////////////////////////////////////////////
//
// Function  : cmd_send
//
// Purpose   : process a "send" command
//
// Inputs    : (none)
//
// Output    : appropriate info
//
// Returns   : nothing
//
// Example   : cmd_send();
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

		static public void cmd_send()
		{
			int amount = 0;
			int new_balance = 0;
			int new_balance2 = 0;
			string username2 = "";
			string sql = "";
			SmartUser recipient;

			MySqlConnection conn = null;
			MySqlTransaction tr = null; 

			if ( !data_fields.ContainsKey("username2") ) {
				send_response(1,"username2 was not specified","");
			}
			username2 = data_fields["username2"];
			if ( !user_index.ContainsKey(username2) ) {
				send_response(1,username2 + " is not a valid username","");
			}
			int u_index = user_index[username2];
			recipient = UsersList[u_index];

			if ( !data_fields.ContainsKey("amount") ) {
				send_response(1,"amount was not specified","");
			}
			
			if ( Regex.IsMatch(data_fields["amount"], "^[0-9]+$") ) {
				try
				{
					amount = Convert.ToInt32(data_fields["amount"]);
				} catch ( Exception ex)
				{
					send_response(1,"Invalid amount",ex.ToString());
				}
			}
			else {
				send_response(1,"Invalid amount","amount is not entirely numeric");
			}

			if ( current_user.username == username2 ) {
				send_response(1,"You are not allowed to send to yourself","");
			}

			if ( current_user.balance < amount ) {
				send_response(1,"Insufficient fundds" , "try a smaller amount");
			}
			if ( recipient.status != "active" ) {
				send_response(1,username2 + " is not an active user. Request denied.","");
			}

			try
			{
				conn = new MySqlConnection(connect_string); 
				conn.Open();
				tr = conn.BeginTransaction();

				MySqlCommand cmd = new MySqlCommand();
				cmd.Connection = conn;
				cmd.Transaction = tr;

// First create the transaction history record

				cmd.CommandText = "UPDATE Authors SET Name='Leo Tolstoy' WHERE Id=1";
				new_balance = current_user.balance - amount;
				new_balance2 = recipient.balance + amount;
				sql = "INSERT INTO smart_users_history " +
						"(mod_date,user1,user1_balance,user2,user2_balance,operation,status,amount) " +
						"VALUES ( now() , " + current_user.id + " , " + new_balance +
						" , " + recipient.id + " , " + new_balance2 + " , 'send' , 'active' , " +
						amount + ")";
				cmd.CommandText = sql;
				cmd.ExecuteNonQuery();

// Next update the information for the sender
				sql = "UPDATE smart_users set balance = " + new_balance +
						" WHERE id = " + current_user.id;
				cmd.CommandText = sql;
				cmd.ExecuteNonQuery();

// Lastly update the information for the recipient
				sql = "UPDATE smart_users set balance = " + new_balance2 +
						" WHERE id = " + recipient.id;
				cmd.CommandText = sql;
				cmd.ExecuteNonQuery();

				tr.Commit();

			} catch (MySqlException ex)
			{
				try 
				{ 
					tr.Rollback();
				} catch (MySqlException ex1) 
				{
					send_response(1,"Database Rollback Error" + ex1.ToString(),ex.ToString());
				}
				send_response(1,"Database Error", ex.ToString() + "\n" + sql);
			} finally
			{
				if (conn != null)
				{
					conn.Close();
				}
			} // finally
			send_response(0,"SUCCESS","send request was successfull");

			return;
		} // cmd_send

//////////////////////////////////////////////////////////////////////
//
// Function  : cmd_adduser
//
// Purpose   : process a "adduser" command
//
// Inputs    : (none)
//
// Output    : appropriate info
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
			int index , missing = 0;
			string sql = "" , missing_fields = "" , sep = "" , fieldname , columns , values;
			string non_numeric_message = "";
			int errors = 0;
			MySqlConnection conn = null;

			columns = "( ";
			values = "values ( ";
			for ( index = 0 ; index < num_new_user_input_fields ; ++index ) {
				fieldname = new_user_input_fields[index];
				if ( data_fields.ContainsKey(fieldname) ) {
					switch ( fieldname )
					{
						case "new_username":
							columns += sep + "username";
							break;
						case "new_password":
							columns += sep + "password";
							break;
						default:
							columns += sep + fieldname;
							break;
					}
					if ( numeric_fields.ContainsKey(fieldname) ) {
						if ( Regex.IsMatch(data_fields[fieldname], "^[0-9]+$"))
							values += sep + data_fields[fieldname];
						else {
							errors += 1;
							non_numeric_message += fieldname + "\n";
						}
					}
					else {
						if ( fieldname == "new_password" ) {
							values += sep + "aes_encrypt('" + data_fields[fieldname] + "','pizza')";
						}
						else {
							values += sep + "'" + data_fields[fieldname] + "'";
						}
					}
				}
				else {
					missing += 1;
					missing_fields += sep + fieldname;
				}
				sep = " , ";
			} // FOR
			if ( missing == 0 && errors == 0 ) {
				columns += sep + "mod_date";
				values += sep + "curdate()";
				columns += sep + "balance";
				values += sep + data_fields["balance1"];
				columns += sep + "status";
				values += sep + "'active'";
				columns += ")";
				values += ")";
				sql = "INSERT INTO smart_users " + columns + " " + values;
				// send_response(0,"SUCCESS","all input fields were found\n" + sql);
				try
				{
					conn = new MySqlConnection(connect_string); 
					conn.Open();

					MySqlCommand cmd = new MySqlCommand();
					cmd.Connection = conn;

					cmd.CommandText = sql;
					cmd.ExecuteNonQuery();
				} catch (MySqlException ex)
				{
					send_response(1,"Database Error", ex.ToString() + "\n" + sql);
				}
				finally
				{
					if (conn != null)
					{
						conn.Close();
					}

				}
				send_response(0,"new user was successfully added","OK");
			} // IF all fields were specified
			else {
				if ( errors == 0 )
					send_response(1,"Some input fields were missing",missing_fields);
				else {
					if ( missing == 0 )
						send_response(1,"Some input fields were invalid",non_numeric_message);
					else
						send_response(1,"Some input fields were missing or invalid",missing_fields + "\n" + non_numeric_message);
				}
			}// ELSE input field issue were detected
			return;
		} // cmd_adduser

//////////////////////////////////////////////////////////////////////
//
// Function  : cmd_deluser
//
// Purpose   : Process "deluser" request
//
// Inputs    : (none)
//
// Output    : (none)
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
			string sql;
			MySqlConnection conn = null;

			if ( !data_fields.ContainsKey("olduser") )
				send_response(1,"Name of existing user was not specified","");

			sql = "UPDATE smart_users SET status = 'expired' WHERE username = '" +
						data_fields["olduser"] + "'";

			try
			{
				conn = new MySqlConnection(connect_string); 
				conn.Open();

				MySqlCommand cmd = new MySqlCommand();
				cmd.Connection = conn;

				cmd.CommandText = sql;
				cmd.ExecuteNonQuery();
			} catch (MySqlException ex)
			{
				send_response(1,"Database Error", ex.ToString() + "\n" + sql);
			}
			finally
			{
				if (conn != null)
				{
					conn.Close();
				}
			}

			send_response(0,"User was successfully marked as expired","");
			return;
		} // end of cmd_deluser

//////////////////////////////////////////////////////////////////////
//
// Function  : cmd_void
//
// Purpose   : Process the requested void command
//
// Inputs    : (none)
//
// Output    : (none)
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
			string sql;
			int tran_id;
			MySqlConnection conn = null;

			if ( !data_fields.ContainsKey("tran_id") )
				send_response(1,"transaction id was not specified","");
			if ( !Regex.IsMatch(data_fields["tran_id"], "^[0-9]+$"))
				send_response(1,"Transaction Id contains non numeric data","");
			tran_id = Convert.ToInt32(data_fields["tran_id"]);
			if ( !trans_index.ContainsKey(tran_id) )
				send_response(1,"Non existant transaction id","");

			sql = "UPDATE smart_users_history SET status = 'voided' WHERE id = " +
						data_fields["tran_id"];

			try
			{
				conn = new MySqlConnection(connect_string); 
				conn.Open();

				MySqlCommand cmd = new MySqlCommand();
				cmd.Connection = conn;

				cmd.CommandText = sql;
				cmd.ExecuteNonQuery();
			} catch (MySqlException ex)
			{
				send_response(1,"Database Error", ex.ToString() + "\n" + sql);
			}
			finally
			{
				if (conn != null)
				{
					conn.Close();
				}
			}
			send_response(0,"The transaction was successfully voided","");

			return;
		} // end of cmd_void

//////////////////////////////////////////////////////////////////////
//
// Function  : Main
//
// Purpose   : program entry point
//
// Inputs    : (none)
//
// Output    : (none)
//
// Returns   : nothing
//
// Example   : Main();
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

		[STAThread] // uses single threaded apartment model (STA)
		static void Main(string[] args) {
		Console.WriteLine("Content-Type: text/xml\n\n");

		response_data = "";
		cmd_priv_level.Add("users",false);
		cmd_priv_level.Add("admins",false);
		cmd_priv_level.Add("hist",false);
		cmd_priv_level.Add("myhist",false);
		cmd_priv_level.Add("send",false);
		cmd_priv_level.Add("adduser",true);
		cmd_priv_level.Add("deluser",true);
		cmd_priv_level.Add("void",true);
		numeric_fields.Add("priv_level" , "privilege level");
		numeric_fields.Add("balance1" , "initial balance");

		parse_input_fields();
		read_smart_users_table();
		read_smart_users_history_table();

		try
		{
			if ( num_vars == 0 ){
				debug_print("No variables");
				send_response(1,"Function code was not specified","No field values were specified");
			}
			else {
				if ( data_fields.ContainsKey("function") ) {
					validate_user_info();
					string function = data_fields["function"];
					if ( !cmd_priv_level.ContainsKey(function) ) {
						send_response(1,"Invalid Command",function + " is not a valid command");
					}
					if ( current_user.priv_level > 0 && cmd_priv_level[function] ) {
						send_response(1,"User does not have sufficient privilege to execute this command","");
					}
					switch (function)
					{
						case "users":
							cmd_users("users");
							break;
						case "admins":
							cmd_users("admins");
							break;
						case "hist":
							cmd_hist("hist");
							break;
						case "myhist":
							cmd_hist("myhist");
							break;
						case "send":
							cmd_send();
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
						default:
							send_response(1,"Unimplemented Command : ",function);
							break;
					}
				}
				else {
					send_response(1,"Function code was not specified","");
				}
			} // ELSE
		} catch ( Exception ex )
		{
			Console.WriteLine(ex.Message);
			send_response(1,"Unexpected Error",ex.Message);
		} finally
		{
		} // finally


		send_response(1,"Control Flow Issue","control flow fell through");
		} // end of Main
	} // end of class ConsoleApplication1

	class SmartUser
	{
		public int id { get; set; }
		public string mod_date { get; set; }
		public string username { get; set; }
		public string password { get; set; }
		public string first_name { get; set; }
		public string last_name { get; set; }
		public string email { get; set; }
		public string phone { get; set; }
		public int priv_level { get; set; }
		public int balance1 { get; set; }
		public int balance { get; set; }
		public string status { get; set; }
		public string comment { get; set; }
	} // end of class SmartUser

	class SmartTrans
	{
		public int id { get; set; }
		public string mod_date { get; set; }
		public int user1 { get; set; }
		public int user1_balance { get; set; }
		public int user2 { get; set; }
		public int user2_balance { get; set; }
		public string operation { get; set; }
		public string status { get; set; }
		public int amount { get; set; }
		public string name1 { get; set; }
		public string name2 { get; set; }
	} // end of class SmartTrans

} // end of namespace ConsoleApplication1
