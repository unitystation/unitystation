#!/usr/bin/python
import subprocess
import sqlite3
from sqlite3 import Error
import sys
import getopt

def create_connection(db_file):
    """ create a database connection to the SQLite database
        specified by the db_file
    :param db_file: database file
    :return: Connection object or None
    """
    try:
        conn = sqlite3.connect(db_file)
        return conn
    except Error as e:
        print(e)
 
    return None
 
def read_bans(conn):
    """
    Query all rows in the tasks table
    :param conn: the Connection object
    :return:
    """
    cur = conn.cursor()
    cur.execute("SELECT * FROM ipblacklist")
 
    rows = cur.fetchall()
 
    for row in rows:
        lst2.append(row)
	
#print len(capture)
lst1=[]
#IP bans
lst2=[]

def blockIP1(ip):
	cmd="/sbin/iptables -A INPUT -s "+ip+" -j DROP"
	subprocess.call(cmd, shell=True)
	for i in range(len(capture)):
		pack=capture[i]
		print pack
		lst1.append(pack['ip'].src)
		#print lst1
		ulst1=set(lst1)
		#print ulst1
		ulst1=list(ulst1)
	for i in range(len(ulst1)):
		ip=ulst1[i]
		if ip in lst2:
			blockIP1(ip)

#Change to loc of ip bnas
def main():
	database = "ipbans.db"
	conn = create_connection(database)
	read_bans(conn)
	blockIP1(ip)

if (__name__== "__main__"):
	main()
