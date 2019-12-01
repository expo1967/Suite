//////////////////////////////////////////////////////////////////////
//
// File      : smart.go
//
// Author    : Barry Kimelman
//
// Created   : November 30, 2019
//
// Purpose   : Go CGI script to manage smart users information
//
// Notes     :
//
//////////////////////////////////////////////////////////////////////

package main

import (
    "database/sql"
    "fmt"
    "os"
	"strings"
	"bufio"

    _ "github.com/go-sql-driver/mysql"
)

var fields_count int
var fields_map map[string]string
var forms_counter int = 0
var darktext2c string = "font-weight: bold; font-size: 16px; font-family: Arial, Times New Roman; overflow: visible; padding-left: 20px; padding-right: 20px; width: 200px; background: gainsboro; color: black;"
var top_level_href = "<BR><A style='font-weight: bold; font-size: 18px;' HREF='/cgi-bin/smart.exe'>Return To Top Level</A>"

//////////////////////////////////////////////////////////////////////
//
// Function  : parse_fields
//
// Purpose   : Parse all the input fields for the CGI script
//
// Inputs    : (none)
//
// Output    : (none)
//
// Returns   : nothing
//
// Example   : parse_fields()
//
// Notes     : Check ENV{"QUERY_STRING"} to determine GET or POST
//
///////////////////////////////////////////////////////////////////////

func parse_fields() {
	fields_count = 0
	fields_map = make(map[string]string)  // initialize the map
	value , exists := os.LookupEnv("QUERY_STRING")
	post_method := true
	if exists {
		if value == "" {
			// fmt.Println("QUERY_STRING is empty")
		} else {
			// fmt.Println("Value of QUERY_STRING => " + value)
			post_method = false
		}
	} else {
		fmt.Println("QUERY_STRING is not an environment variable")
	}
	if post_method {
		// fmt.Println("<BR>Read POST method data<BR>")
		reader := bufio.NewReader(os.Stdin)
		for {
			looptext, _ := reader.ReadString('\n')
			count := len(looptext)
			if count == 0 {
				break
			}
			// fmt.Print("Without newline [" + looptext + "]\n")
			post_vars := strings.Split(looptext, "&")
			for _, pvar := range post_vars {
				pair := strings.SplitN(pvar, "=", 2)
				// fmt.Println("<BR>POST Var : name = " + pair[0] + " , value = " + pair[1])
				fields_count += 1
				fields_map[pair[0]] = pair[1]
			}
		}

	} else {
		// fmt.Println("<BR>Parse QUERY_STRING fields<BR>")
		query_vars := strings.Split(value, "&")
		for _, qvar := range query_vars {
			pair := strings.SplitN(qvar, "=", 2)
			// fmt.Println("<BR>Query Var : name = " + pair[0] + " , value = " + pair[1])
			fields_map[pair[0]] = pair[1]
			fields_count += 1
		}
	}

} // parse_fields

//////////////////////////////////////////////////////////////////////
//
// Function  : generate_menu_entry
//
// Purpose   : Generate a menu entry
//
// Inputs    : menu_title string - title for submit button
//             func_string string - function code string for hidden field
//
// Output    : menu entry
//
// Returns   : nothing
//
// Example   : generate_menu_entry("List Metadata","meta")
//
// Notes     :
//
///////////////////////////////////////////////////////////////////////

func generate_menu_entry(menu_title string, func_string string) {
	var form_name string
	
	forms_counter += 1
	form_name = fmt.Sprintf("form_%d",forms_counter)
	fmt.Printf("<FORM name='%s' id='%s' method='POST' action='/cgi-bin/smart.exe'>\n",form_name,form_name)
	fmt.Printf("<input type='hidden' name='function' id='function' value='%s'>\n",func_string)
	fmt.Printf("<input style='%s' type='submit' value='%s'>\n",darktext2c,menu_title)
	fmt.Println("</FORM><BR>")

} // generate_menu_entry

//////////////////////////////////////////////////////////////////////
//
// Function  : generate_main_menu
//
// Purpose   : Display the main menu
//
// Inputs    : (none)
//
// Output    : (none)
//
// Returns   : nothing
//
// Example   : generate_main_menu()
//
// Notes     :
//
///////////////////////////////////////////////////////////////////////

func generate_main_menu() {

	generate_menu_entry("List Metadata","meta")
	generate_menu_entry("List Users","users")
	generate_menu_entry("List History","history")

} // generate_main_menu

//////////////////////////////////////////////////////////////////////
//
// Function  : fatal_error
//
// Purpose   : Display a fatal error message and exit
//
// Inputs    : error string
//
// Output    : error message
//
// Returns   : nothing
//
// Example   : fatal_error()
//
// Notes     : Program execution is terminated
//
///////////////////////////////////////////////////////////////////////

func fatal_error(error string) {

	fmt.Printf("<BR><H2>Fatal Error<BR>%s</H2>\n",error)
	os.Exit(0)
} // fatal_error

//////////////////////////////////////////////////////////////////////
//
// Function  : display_table_metadata
//
// Purpose   : Process a "meta" command
//
// Inputs    : table_name string
//
// Output    : metadata informnation
//
// Returns   : nothing
//
// Example   : display_table_metadata()
//
// Notes     : (none)
//
///////////////////////////////////////////////////////////////////////

func display_table_metadata(table_name string) {
	fmt.Printf("<H3>display metadata information for table '%s'</H3>\n", table_name)

    db, err := sql.Open("mysql", "root:archer-nx01@(127.0.0.1:3306)/qwlc?parseTime=true")
    if err != nil {
		fmt.Printf("<BR><BR><H2>Fatal Error<BR>%v</H2>\n",err)
		os.Exit(0)
    }
    if err := db.Ping(); err != nil {
		fmt.Printf("<BR><BR><H2>Fatal Error<BR>%v</H2>\n",err)
		os.Exit(0)
	}
// Query the columns of the named table
       var (
           ordinal        int
           colname        string
           isnull         string
           maxlen         string
		column_type    string
		extra          string
		column_key     string
		comment        string
       )
	query := "select ordinal_position ordinal, column_name colname,is_nullable isnull," +
				"ifnull(character_maximum_length,'--') maxlen,column_type,extra,column_key," +
				"ifnull(column_comment,'--') comment" +
				" from information_schema.columns where table_schema = 'qwlc' and " +
				"table_name = ?"
    rows, err := db.Query(query,table_name)
	if err != nil {
		fmt.Printf("<BR><BR><H2>Fatal Error<BR>%v</H2>\n",err)
		os.Exit(0)
	}
	defer rows.Close()
	num_cols := 0
	fmt.Println("<TABLE border='1' cellspacing='0' cellpadding='3'>")
	fmt.Println("<THEAD>")
	fmt.Println("<TR style='background: gainsboro;'><TH>Ordinal</TH><TH>Colname</TH><TH>Data Type</TH><TH>Maxlen</TH>")
	fmt.Println("<TH>Nullable ?</TH><TH>Key</TH><TH>Extra</TH><TH>Comment</TH></TR>")
	fmt.Println("</THEAD>")
	fmt.Println("<TBODY>")
	for rows.Next() {
		err := rows.Scan(&ordinal, &colname, &isnull, &maxlen, &column_type, &extra, &column_key, &comment)
		if err != nil {
			fmt.Printf("<BR><BR><H2>Fatal Error<BR>%v</H2>\n",err)
			os.Exit(0)
		}
		num_cols += 1
		fmt.Printf("<TR><TD>%d</TD><TD>%s</TD><TD>%s</TD><TD>%s</TD><TD>%s</TD><TD>%s</TD><TD>%s</TD><TD>%s</TD>\n",
				ordinal,colname,column_type,maxlen,isnull,column_key,extra,comment)
	} // for
	fmt.Println("</TBODY>")
	fmt.Println("</TABLE><BR>")

} // display_table_metadata

//////////////////////////////////////////////////////////////////////
//
// Function  : list_metadata
//
// Purpose   : Process a "meta" command
//
// Inputs    : (none)
//
// Output    : metadata informnation
//
// Returns   : nothing
//
// Example   : list_metadata()
//
// Notes     : (none)
//
///////////////////////////////////////////////////////////////////////

func list_metadata() {
	display_table_metadata("smart_users")
	display_table_metadata("smart_users_history")
} // list_metadata

//////////////////////////////////////////////////////////////////////
//
// Function  : list_history
//
// Purpose   : Process a "history" command
//
// Inputs    : (none)
//
// Output    : history informaion
//
// Returns   : nothing
//
// Example   : list_history()
//
// Notes     : (none)
//
///////////////////////////////////////////////////////////////////////

func list_history() {
	fmt.Println("<H3>display history information</H3>")
} // list_history

//////////////////////////////////////////////////////////////////////
//
// Function  : list_users
//
// Purpose   : Process a "users" command
//
// Inputs    : (none)
//
// Output    : history informaion
//
// Returns   : nothing
//
// Example   : list_users()
//
// Notes     : (none)
//
///////////////////////////////////////////////////////////////////////

func list_users() {
	fmt.Println("<H3>display users information</H3>")
} // list_users

//////////////////////////////////////////////////////////////////////
//
// Function  : main
//
// Purpose   : Display CGI environment
//
// Inputs    : (none)
//
// Output    : env vars and field values
//
// Returns   : nothing
//
// Example   : main()
//
// Notes     : (none)
//
///////////////////////////////////////////////////////////////////////

func main() {

    fmt.Println("Content-Type: text/html\n");
    fmt.Println("<HTML>")
    fmt.Println("<HEAD>")
	fmt.Println("<TITLE>Smart Users Management</TITLE>")
	fmt.Println("</HEAD>")
	fmt.Println("<BODY>")
	fmt.Println("<div style='padding-left: 20px;'>")
	fmt.Println("<H2>Smart Users Management</H2>")

	parse_fields()
	f_code, ok := fields_map["function"] // test for existance of function code
	if ok {
		// fmt.Printf("<H3>Execute function %s</H3>\n",f_code)
		switch f_code {
			case "meta":
				list_metadata()
				break
			case "users":
				list_users()
				break
			case "history":
				list_history()
				break
			default:
				fmt.Printf("<H3>Received invalid function code '%s'</H3>\n",f_code)
		} // switch
		fmt.Printf("<BR><BR>%s<BR><BR>",top_level_href)
	} else {
		generate_main_menu()
	}

	fmt.Println("</div>")
	fmt.Println("</BODY>")
	fmt.Println("</HTML>")
}