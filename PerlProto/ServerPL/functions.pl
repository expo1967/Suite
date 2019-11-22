
my %functions = (

	"users" => { "function" => \&show_users , "admin" => 0 } ,

	"void" => { "function" => \&void_trans , "admin" => 1 } ,

	"send" => { "function" => \&send_money , "admin" => 1 } ,

	"hist" => { "function" => \&show_trans , "admin" => 0 } ,

	"delete_user" => { "function" => \&delete_user , "admin" => 1 } ,

	"get_user" => { "function" => \&get_user , "admin" => 1 } ,

	"modify_user" => { "function" => \&modify_user , "admin" => 1 } ,

	"add_user" => { "function" => \&add_user , "admin" => 1 } ,

);
