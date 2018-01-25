import pyshark
import subprocess
capture = pyshark.LiveCapture(interface='eth0')
capture.sniff(timeout=50)
#print len(capture)
lst1=[]
#IP address exclude, so below ip will not be blocked
lst2=['192.168.4.138']
def blockip(ip):
	cmd="/sbin/iptables -A INPUT -s "+ip+" -j DROP"
	print cmd
	subprocess.call(cmd,shell=True)
 
def blockIP1(ip):
	cmd="/sbin/iptables -A INPUT -s "+ip+" -j DROP"
	subprocess.call(cmd, shell=True)
	subprocess.call("kill -9 $(/usr/bin/pgrep dumpcap)", shell=True)
	subprocess.call("/usr/bin/kill -9 $(/usr/bin/pgrep tshark)", shell=True)
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
		if ip not in lst2:
			blockIP1(ip)
subprocess.call("kill -9 $(/usr/bin/pgrep dumpcap)", shell=True)
subprocess.call("/usr/bin/kill -9 $(/usr/bin/pgrep tshark)", shell=True)
