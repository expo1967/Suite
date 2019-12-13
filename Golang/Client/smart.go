//////////////////////////////////////////////////////////////////////
//
// File      : smart.go
//
// Author    : Barry Kimelman
//
// Created   : December 7, 2019
//
// Purpose   : Smart Contracts client interface
//
// Notes     :
//
//////////////////////////////////////////////////////////////////////

package main

import (
	"os"
    "fmt"
    "regexp"
	"bufio"
	"strconv"
    "golang.org/x/text/language"
    "golang.org/x/text/message"
	"net/http"
	"golang.org/x/crypto/ssh/terminal"
	"syscall"
	"strings"
)

var debug_mode bool = true

var num_args int
var xml_buffer string
var trans_fields = [11] string { "id" , "mod_date" , "user1" , "user1_balance" , "user2" ,
			"user2_balance" , "operation" , "status" , "amount" , "name1" , "name2" }
var user_fields = [13] string { "id" , "mod_date" , "username" , "password" , "first_name" ,
			"last_name" , "email" , "phone" , "priv_level" , "balance1" , "balance" ,
			"status" , "comment" }
var new_user_fields = [9] string { "username" , "password" , "first_name" ,
			"last_name" , "email" , "phone" , "priv_level" , "balance1" , "comment" }

var admin_user_fields = [5] string { "username" , "first_name" , "last_name" , "email" , "phone" }
var balance_fields = [1] string { "balance" }
var field_values map[string]string
var status_fields = [3] string { "status_code" , "error_message" , "error_details" }
var status_values map[string]string
var username string
var password string
var command string
var url_part_1 = "http://localhost:88/cgi-bin2/smart.cgi?"
// var url_part_1 = "http://localhost:88/cgi-bin/funds2-server.exe?"

//////////////////////////////////////////////////////////////////////
//
// Function  : format_money
//
// Purpose   : Convert a number of pennies to a formatted amount with a 'S' and commas and
//             print the value
//
// Inputs    : pennies int - number of pennies
//
// Output    : formattedmoney value
//
// Returns   : the formmated currency string
//
// Example   : currency = format_money(150075) // should get $1500.75
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

func format_money(pennies int) string {

	cents := pennies % 100
	dollars := pennies / 100
	p := message.NewPrinter(language.English)
	
	dol_value := p.Sprintf("%d",dollars)
	result := fmt.Sprintf("$%s%s%02d",dol_value,".",cents)

	return(result)
} // format_money

//////////////////////////////////////////////////////////////////////
//
// Function  : parse_fields
//
// Purpose   : Parse a set 
//
// Inputs    : tag_name string - "<TRANS>" or "<USER>"
//             tag_value string - the <TRANS> or <USER> tag data
//
// Output    : appropriate messages
//
// Returns   : number of errors
//
// Example   : num_errors = parse_fields("<USER>",matches[index]);
//
// Notes     : If the status code indicates an error then program execution will
//             be terminated
//
//////////////////////////////////////////////////////////////////////

func parse_fields(tag_name string, tag_value string) int {

	var field_name string
	var num_fields int

	if ( tag_name == "<USER>") {
		num_fields = 13
	} else {
		num_fields = 11
	}

	errors := 0
	for index2 := 0 ; index2 < num_fields ; index2 ++ {
		if ( tag_name == "<USER>") {
			field_name = user_fields[index2]
		} else {
			field_name = trans_fields[index2]
		}
		pattern := fmt.Sprintf("(?i)<%s>(.*?)</%s>",field_name,field_name)
		re2 := regexp.MustCompile(pattern)
		rs := re2.FindStringSubmatch(tag_value)
		count2 := len(rs)
		if count2 > 0 {
			// fmt.Printf("Value for field %s is %q\n",field_name,rs[0])
			// fmt.Printf("Paren Data for field %s is %q\n",field_name,rs[1])
			field_values[field_name] = rs[1]
		} else {
			fmt.Printf("Could not find field %s\n",field_name)
			errors += 1
		}
	} // FOR parsing each of the <TRANS> or <USER>fields

	return(errors)
}

//////////////////////////////////////////////////////////////////////
//
// Function  : send_request
//
// Purpose   : send a request to the server
//
// Inputs    : string url - the URL to be sent as a GET request
//
// Output    : appropriate messages
//
// Returns   : nothing
//
// Example   : send_request(url);
//
// Notes     : If the status code indicates an error then program execution will
//             be terminated
//
//////////////////////////////////////////////////////////////////////

func send_request(url string) {

	field_values = make(map[string]string)
	status_values = make(map[string]string)
    resp, err := http.Get(url)
    if err != nil {
        panic(err)
    }
    defer resp.Body.Close()

    fmt.Printf("Response status from %s :\n%s\n", url,resp.Status)

	xml_buffer = ""
    scanner := bufio.NewScanner(resp.Body)
    for num_records := 0; scanner.Scan() ; num_records++ {
		xml_buffer += scanner.Text()
    }

    if err := scanner.Err(); err != nil {
        panic(err)
    }
	num_bytes := len(xml_buffer)
	fmt.Printf("\n%d bytes of data retrieved for request\n%s\n",num_bytes,url)
	if debug_mode {
		fmt.Printf("%s\n",xml_buffer)
	}

	re_status := regexp.MustCompile("(?m)<STATUS>.*?</STATUS>")
	status_matches := re_status.FindAllString(xml_buffer,-1)
	// status_count := len(status_matches)
	// fmt.Printf("\n%d matches from the records for the status info\n",status_count)
	// fmt.Printf("%q\n",status_matches)
	for index3 := 0 ; index3 < 3 ; index3++ {
		status_field := status_fields[index3];
		status_pattern := fmt.Sprintf("(?i)<%s>(.*?)</%s>",status_field,status_field)
		re3 := regexp.MustCompile(status_pattern)
		rs3 := re3.FindStringSubmatch(status_matches[0])
		count3 := len(rs3)
		if ( count3 > 0 ) {
			// fmt.Printf("Value for status field '%s' is '%s'\n",status_field,rs3[0])
			status_values[status_field] = rs3[1]
		} else {
			fmt.Printf("Nothing found for status field '%s'\n",status_field)
		}
	} // FOR over status fields
	// fmt.Printf("\nHash contents for status info are\n%q\n",status_values)
	if s_code, err := strconv.Atoi(status_values["status_code"]); err == nil {
		// fmt.Printf("\n'%s' : [%T], [%v]\n", status_values["status_code"],s_code, s_code)
		fmt.Printf("\nStatus Code = %d\n",s_code)
		fmt.Printf("%s\n",status_values["error_message"])
		fmt.Printf("%s\n",status_values["error_details"])
		if ( s_code != 0 ) {
			os.Exit(1)
		}
	} else {
		fmt.Printf("\nCan't convert status code value '%s' to int\n%v\n",status_values["status_code"],err);
		os.Exit(1)
	}

	return
} // send_request

//////////////////////////////////////////////////////////////////////
//
// Function  : cmd_hist
//
// Purpose   : process a "hist" command
//
// Inputs    : func_code string - "hist" or "myhist"
//
// Output    : appropriate messages
//
// Returns   : nothing
//
// Example   : cmd_hist("hist")
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

func cmd_hist(func_code string) {
	var url string

	url += url_part_1 + "function=" + func_code + "&username=" + username + "&password=" + password
	send_request(url)

	re := regexp.MustCompile("(?mi)<TRANS>.*?</TRANS>")
	matches := re.FindAllString(xml_buffer,-1)
	count := len(matches)
	fmt.Printf("\n%d transaction history records were retrieved\n",count)

	for index := 0 ; index < count ; index ++ {
		fmt.Printf("\nProcess history record %d of %d\n",1+index,count)
		num_errors := parse_fields("<TRANS>",matches[index]);
		if ( num_errors > 0 ) {
			fmt.Printf("\n%d errors detected in <TRANS> tag\n%s\n",num_errors,matches[index])
			os.Exit(1)
		}

		fmt.Printf("Id            : %s\n",field_values["id"])
		fmt.Printf("mod_date      : %s\n",field_values["mod_date"])
		fmt.Printf("user1         : %s\n",field_values["user1"])
		balance1 := field_values["user1_balance"]
		fmt.Printf("user1_balance : ")
		bal1, err1 := strconv.Atoi(balance1)
		if err1 != nil {
			fmt.Printf("Error converting user1_balance '%s' to integer %q\n",balance1,err1)
			os.Exit(1)
		}
		bal1_dollars := format_money(bal1)
		fmt.Printf("%s\n",bal1_dollars)

		fmt.Printf("user2         : %s\n",field_values["user2"])
		balance2 := field_values["user2_balance"]
		fmt.Printf("user2_balance : ")
		bal2, err2 := strconv.Atoi(balance2)
		if err2 != nil {
			fmt.Printf("Error converting user2_balance '%s' to integer %q\n",balance2,err2)
			os.Exit(1)
		}
		bal2_dollars := format_money(bal2)
		fmt.Printf("%s\n",bal2_dollars)

		fmt.Printf("operation     : %s\n",field_values["operation"])
		fmt.Printf("status        : %s\n",field_values["status"])
		fmt.Printf("amount        : %s\n",field_values["amount"])
		fmt.Printf("name1         : %s\n",field_values["name1"])
		fmt.Printf("name2         : %s\n",field_values["name2"])

	} // FOR over each <TRAN>

	return
} // cmd_hist

//////////////////////////////////////////////////////////////////////
//
// Function  : cmd_users
//
// Purpose   : process a "users" command
//
// Inputs    : cmd_code string - command code ("users" or "myself")
//
// Output    : appropriate messages
//
// Returns   : nothing
//
// Example   : cmd_users("users")
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

func cmd_users(func_code string) {
	var url string
	var user_indices map[string]int
	var usernames []string
	var u_name string

	url += url_part_1 + "function=" + func_code + "&username=" + username + "&password=" + password
	send_request(url)

	user_fields_titles := map[string]string {
		"id": "Id" , "mod_date": "Mod Date" , "username": "Username" , "password": "Password" ,
		"first_name": "First Name" , "last_name": "Last Name" , "email": "Email" , "phone": "Phone" ,
		"priv_level": "Privilege Level" , "balance1": "Initial Balance" , "balance": "Current Balance" ,
		"status": "Status" , "comment": "Comment",
	}

	re := regexp.MustCompile("(?mi)<USER>.*?</USER>")
	matches := re.FindAllString(xml_buffer,-1)
	count := len(matches)
	fmt.Printf("\n%d user records were retrieved\n",count)
	user_indices = make(map[string]int)

	for index := 0 ; index < count ; index ++ {
		fmt.Printf("\nProcess user entry %d of %d\n",1+index,count)
		num_errors := parse_fields("<USER>",matches[index]);
		if ( num_errors > 0 ) {
			fmt.Printf("\n%d errors detected in <USER> tag\n%s\n",num_errors,matches[index])
			os.Exit(1)
		}
		for index2 := 0 ; index2 < 12 ; index2++ {
			field_name := user_fields[index2]
			if ( field_name == "balance1" || field_name == "balance") {
				s_code, err := strconv.Atoi(field_values[field_name])
				if err != nil {
					fmt.Printf("Can't convert value '%s' to int for field %s\n",field_values[field_name],field_name)
				} else {
					currency := format_money(s_code)
					fmt.Printf("%-16.16s : %s\n",user_fields_titles[field_name],currency)
				}
			} else {
				fmt.Printf("%-16.16s : %s\n",user_fields_titles[field_name],field_values[field_name])
			}
			u_name = field_values["username"]
			user_indices[u_name] = index
			usernames = append(usernames, u_name)
		} // FOR over USER fields
	} // FOR over <USER> tags

	return
} // cmd_users

//////////////////////////////////////////////////////////////////////
//
// Function  : cmd_admins
//
// Purpose   : process a "admins" command
// Purpose   : process a "admins" command
//
// Inputs    : (none)
//
// Output    : appropriate messages
//
// Returns   : nothing
//
// Example   : cmd_admins()
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

func cmd_admins() {
	var url string

	url += url_part_1 + "function=admins&username=" + username + "&password=" + password
	send_request(url)

	re := regexp.MustCompile("(?mi)<USER>.*?</USER>")
	matches := re.FindAllString(xml_buffer,-1)
	count := len(matches)
	fmt.Printf("\n%d administrator records were retrieved\n",count)

	for index := 0 ; index < count ; index ++ {
		fmt.Printf("\nAdmin entry %d of %d\n",1+index,count)
		num_errors := parse_fields("<USER>",matches[index]);
		if ( num_errors > 0 ) {
			fmt.Printf("\n%d errors detected in <USER> tag\n%s\n",num_errors,matches[index])
			os.Exit(1)
		}
		fmt.Printf("Username : %s , ",field_values["username"])
		fmt.Printf("Name : %s %s",field_values["first_name"],field_values["last_name"])
		fmt.Printf("\nEmail : %s , Phone : %s\n",field_values["email"],field_values["phone"])
	} // FOR over <USER> tags

	return
} // cmd_admins

//////////////////////////////////////////////////////////////////////
//
// Function  : cmd_send
//
// Purpose   : process a "users" command
//
// Inputs    : (none)
//
// Output    : appropriate messages
//
// Returns   : nothing
//
// Example   : cmd_send()
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

func cmd_send() {

	var url string

	if ( num_args < 6 ){
		fmt.Printf("Usage : %s username password send recipient amount\n",os.Args[0])
		os.Exit(1)
	}

	url += url_part_1 + "function=send&username=" + username + "&password=" + password +
				"&username2=" + os.Args[4] + "&amount=" + os.Args[5]
	send_request(url)

	return
} // cmd_send

//////////////////////////////////////////////////////////////////////
//
// Function  : cmd_balance
//
// Purpose   : process a "balance" command
//
// Inputs    : (none)
//
// Output    : appropriate messages
//
// Returns   : nothing
//
// Example   : cmd_balance()
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

func cmd_balance() {
	var url string

	url += url_part_1 + "function=balance&username=" + username + "&password=" + password
	send_request(url)

	re := regexp.MustCompile("(?mi)<BALANCE>.*?</BALANCE>")
	matches := re.FindAllString(xml_buffer,-1)
	count := len(matches)
	fmt.Printf("\n%d balance records were retrieved\n",count)
	pattern := "(?i)<BALANCE>(.*?)</BALANCE>"

	re2 := regexp.MustCompile(pattern)
	rs := re2.FindStringSubmatch(matches[0])
	count2 := len(rs)
	if count2 > 0 {
		// fmt.Printf("Value for field %s is %q\n",field_name,rs[0])
		// fmt.Printf("Paren Data for field %s is %q\n",field_name,rs[1])
		if currency, err := strconv.Atoi(rs[1]); err == nil {
			money := format_money(currency)
			fmt.Printf("\nTotal System Balance = %s\n",money)
		} else {
			fmt.Printf("\nCan't convert balance value '%s' to int\n%v\n",rs[1],err);
			os.Exit(1)
		}

	} else {
		fmt.Printf("Could not find field %s\n","BALANCE")
	}

	return
} // cmd_balance

//////////////////////////////////////////////////////////////////////
//
// Function  : cmd_deluser
//
// Purpose   : process a "deluser" command
//
// Inputs    : (none)
//
// Output    : appropriate messages
//
// Returns   : nothing
//
// Example   : cmd_deluser()
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

func cmd_deluser() {

	var url string

	if ( num_args < 5 ){
		fmt.Printf("Usage : %s username password send old_user\n",os.Args[0])
		os.Exit(1)
	}

	url += url_part_1 + "function=deluser&username=" + username + "&password=" + password +
				"&olduser=" + os.Args[4]
	send_request(url)

	return
} // cmd_deluser

//////////////////////////////////////////////////////////////////////
//
// Function  : cmd_adduser
//
// Purpose   : process a "adduser" command
//
// Inputs    : (none)
//
// Output    : appropriate messages
//
// Returns   : nothing
//
// Example   : cmd_adduser()
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

func cmd_adduser() {

	var url string

	user_fields_titles := map[string]string {
		"username": "Username" , "password": "Password" ,
		"first_name": "First Name" , "last_name": "Last Name" , "email": "Email" , "phone": "Phone" ,
		"priv_level": "Privilege Level" , "balance1": "Initial Balance" ,
		"comment": "Comment",
	}
	url += url_part_1 + "function=adduser&username=" + username + "&password=" + password;

	num_fields := len(new_user_fields)
	reader := bufio.NewReader(os.Stdin)
	for index:= 0 ; index < num_fields ; index++ {
		field_name := new_user_fields[index]
		field_title := user_fields_titles[field_name]
		fmt.Printf("Enter value for [%s] %s : ",field_name,field_title)
		if ( field_name == "password" ) {
			bytePassword, err := terminal.ReadPassword(int(syscall.Stdin))
			if err != nil {
				fmt.Printf("Error reading password\n%q\n",err)
				return
			}
			password := string(bytePassword)
			url += "&new_password=" + password
			fmt.Printf("\n")
		} else {
			stuff, _ := reader.ReadString('\n')
			stuff = strings.TrimSpace(stuff)
			if ( field_name == "username" ) {
				url += "&new_username=" + stuff
			} else {
				url += "&" + field_name + "=" + stuff
			}
		}
	} // FOR
	fmt.Printf("\nURL = %s\n",url)
	send_request(url)

	return
} // cmd_adduser

//////////////////////////////////////////////////////////////////////
//
// Function  : main
//
// Purpose   : main
//
// Inputs    : command line arguments
//
// Output    : appropriate messages
//
// Returns   : nothing
//
// Example   : smart.exe username password command [option [... option]
//
// Notes     : (none)
//
//////////////////////////////////////////////////////////////////////

func main() {

	num_args = len(os.Args)
	if ( num_args < 4 ){
		fmt.Printf("Usage : %s username password command [option [... option]]\n",os.Args[0])
		os.Exit(1)
	}
	username = os.Args[1]
	password = os.Args[2]
	command = os.Args[3]
	switch command {
	case "hist":
		cmd_hist("hist")
		break
	case "myhist":
		cmd_hist("myhist")
		break
	case "users":
		cmd_users("users")
		break
	case "myself":
		cmd_users("myself")
		break
	case "send":
		cmd_send()
		break
	case "balance":
		cmd_balance()
		break
	case "admins":
		cmd_admins()
		break
	case "deluser":
		cmd_deluser()
		break
	case "adduser":
		cmd_adduser()
		break
	default:
		fmt.Printf("'%s' is not a valid command\n",command)
		os.Exit(1)
	}

} // main
