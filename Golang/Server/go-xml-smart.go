//////////////////////////////////////////////////////////////////////
//
// File      : go-xml-smart.go
//
// Author    : Barry Kimelman
//
// Created   : December 12, 2019
//
// Purpose   : Go CGI script to generate XML output for Smart Contracts
//
// Notes     :
//
//////////////////////////////////////////////////////////////////////

package main

import (
    "fmt"
    "os"
	"strings"
	"bufio"
	"database/sql"

   _ "github.com/go-sql-driver/mysql"
)

var fields_count int
var fields_map map[string]string
var function string

type TransRec struct {
	id				int
	mod_date		string
	user1			int
	user1_balance	int
	user2			int
	user2_balance	int
	operation		string
	status			string
	amount			int
	void_date		string
	name1			string  // calcualted field
	name2			string  // calculated field
}
var trans_rec TransRec
var history []TransRec
var num_hist int

type UserRec struct {
	id			int
	mod_date	string
	username	string
	password	string
	first_name	string
	last_name	string
	email		string
	phone		string
	priv_level	int
	balance1	int
	balance		int
	status		string
	comment		string
}
var user_rec UserRec
var users []UserRec
var num_users int

type StatusInfo struct {
	status_code		int
	error_message	string
	error_details	string
}
var status_info StatusInfo

var response_data string

var mysql_connect_string string

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
		// fmt.Println("QUERY_STRING is not an environment variable")
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
// Function  : send_response
//
// Purpose   : Read contents of Smart Users table into an array of structures
//
// Inputs    : (none)
//
// Output    : (none)
//
// Returns   : nothing
//
// Example   : send_response()
//
// Notes     : Send a response back to the client
//
///////////////////////////////////////////////////////////////////////

func send_response() {

	fmt.Printf("<RESPONSE>\n<STATUS>\n")
	fmt.Printf("<status_code>%d</status_code>\n",status_info.status_code)
	fmt.Printf("<error_message>%s</error_message>\n",status_info.error_message)
	fmt.Printf("<error_details>%s</error_details>\n",status_info.error_details)
	fmt.Printf("</STATUS>\n")
	if response_data != "" {
		fmt.Printf("%s",response_data)
	}
	fmt.Printf("</RESPONSE>")
	os.Exit(0)
} // send_response

//////////////////////////////////////////////////////////////////////
//
// Function  : read_smart_users_table
//
// Purpose   : Read contents of Smart Users table into an array of structures
//
// Inputs    : (none)
//
// Output    : (none)
//
// Returns   : nothing
//
// Example   : read_smart_users_table()
//
// Notes     : Upon error detection an error response is sent back to the client
//
///////////////////////////////////////////////////////////////////////

func read_smart_users_table() {
	var buffer string

	num_users = 0
    db, err := sql.Open("mysql", mysql_connect_string)
    if err != nil {
		status_info.status_code = 1
		status_info.error_message = "database error. failed to connect"
		buffer = fmt.Sprintf("%q",err)
		status_info.error_details = buffer
		send_response()
    }
    if err := db.Ping(); err != nil {
		status_info.status_code = 1
		status_info.error_message = "database error. failed to ping"
		buffer = fmt.Sprintf("%q",err)
		status_info.error_details = buffer
		send_response()
    }

	query := `select id,mod_date,username,aes_decrypt(password,'pizza') password,first_name,
last_name,email,phone,priv_level,balance1,balance,status,comment
from smart_users`

	results, err := db.Query(query)
       if err != nil {
		status_info.status_code = 1
		status_info.error_message = "database error. Query failed"
		buffer = fmt.Sprintf("%q",err)
		status_info.error_details = buffer
		send_response()
	}

	for results.Next() {
		num_users += 1
		err = results.Scan(&user_rec.id,&user_rec.mod_date,&user_rec.username,&user_rec.password,
					&user_rec.first_name,&user_rec.last_name,&user_rec.email,&user_rec.phone,
					&user_rec.priv_level,&user_rec.balance1,&user_rec.balance,
					&user_rec.status,&user_rec.comment)
		if err != nil {
			status_info.status_code = 1
			status_info.error_message = "database error. failed to scan record data"
			buffer = fmt.Sprintf("%q",err)
			status_info.error_details = buffer
			send_response()
		}
		users = append(users,user_rec)
	} // FOR

	return
} // read_smart_users_table

//////////////////////////////////////////////////////////////////////
//
// Function  : read_smart_history_table
//
// Purpose   : Read contents of Smart Users History table into an array of structures
//
// Inputs    : (none)
//
// Output    : (none)
//
// Returns   : nothing
//
// Example   : read_smart_history_table()
//
// Notes     : Upon error detection an error response is sent back to the client
//
///////////////////////////////////////////////////////////////////////

func read_smart_history_table() {
	var buffer string

	num_hist = 0
    db, err := sql.Open("mysql", mysql_connect_string)
    if err != nil {
		status_info.status_code = 1
		status_info.error_message = "database error. failed to connect"
		buffer = fmt.Sprintf("%q",err)
		status_info.error_details = buffer
		send_response()
    }
    if err := db.Ping(); err != nil {
		status_info.status_code = 1
		status_info.error_message = "database error. failed to ping"
		buffer = fmt.Sprintf("%q",err)
		status_info.error_details = buffer
		send_response()
    }

	query := `select id,mod_date,user1,user1_balance bal1,user2,user2_balance bal2,operation,status,amount,void_date,
(select username from smart_users where id = h.user1) name1,
(select username from smart_users where id = h.user2) name2
from smart_users_history h`

	results, err := db.Query(query)
       if err != nil {
		status_info.status_code = 1
		status_info.error_message = "database error. Query failed"
		buffer = fmt.Sprintf("%q",err)
		status_info.error_details = buffer
		send_response()
	}

	for results.Next() {
		num_hist += 1
		err = results.Scan(&trans_rec.id,&trans_rec.mod_date,&trans_rec.user1,&trans_rec.user1_balance,
					&trans_rec.user2,&trans_rec.user2_balance,&trans_rec.operation,&trans_rec.status,
					&trans_rec.amount,&trans_rec.void_date,&trans_rec.name1,&trans_rec.name2)
		if err != nil {
			status_info.status_code = 1
			status_info.error_message = "database error. failed to scan record data"
			buffer = fmt.Sprintf("%q",err)
			status_info.error_details = buffer
			send_response()
		}
		history = append(history,trans_rec)
	} // FOR

	return
} // read_smart_history_table

//////////////////////////////////////////////////////////////////////
//
// Function  : display_input_fields
//
// Purpose   : Display input fields passed to CGI script
//
// Inputs    : (none)
//
// Output    : input fields
//
// Returns   : nothing
//
// Example   : display_input_fields()
//
// Notes     : (none)
//
///////////////////////////////////////////////////////////////////////

func display_input_fields() {
	fmt.Println("<FIELDS>")

	for key, value := range fields_map {
		fmt.Printf("<NAME>%s</NAME><VALUE>%s</VALUE>\n",key,value)
	}

	fmt.Println("</FIELDS>")

} // display_input_fields

//////////////////////////////////////////////////////////////////////
//
// Function  : main
//
// Purpose   : Go CGI script to generate XML output for Smart Contracts
//
// Inputs    : (none)
//
// Output    : XML processing results
//
// Returns   : nothing
//
// Example   : main()
//
// Notes     : (none)
//
///////////////////////////////////////////////////////////////////////

func main() {

	var ok bool

	status_info.status_code = 0
	status_info.error_message = ""
	status_info.error_details = ""
	response_data = ""
	mysql_connect_string = "root:archer-nx01@(127.0.0.1:3306)/qwlc?parseTime=true"

    fmt.Println("Content-Type: text/xml\n");

	read_smart_history_table()
	read_smart_users_table()

	parse_fields()

	function, ok = fields_map["function"]
	if ok {
		response_data += fmt.Sprintf("<FUNCTION>%s</FUNCTION>\n",function)
	} else {
		status_info.status_code = 1
		status_info.error_message = "No value specified for function code"
		status_info.error_details = ""
		send_response()
	}

	status_info.status_code = 0
	status_info.error_message = "We are good"
	status_info.error_details = "no complaints"
	response_data += fmt.Sprintf("<NUM_USERS>%d</NUM_USERS>\n",num_users)
	response_data += fmt.Sprintf("<NUM_HIST>%d</NUM_HIST>\n",num_hist)
	send_response()
} // main