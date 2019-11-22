select count(*) num_users from smart_users;
select count(*) num_hist from smart_users_history;
select count(*) balance_mismatch from smart_users where balance1 != balance;
