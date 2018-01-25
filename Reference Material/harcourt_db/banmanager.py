
import sys
import getopt
import subprocess

_name    = "banman"
_version = "0.1"

def main(argv):
	try:
		opts, args = getopt.getopt(argv, "ha:d:lv", ["help", "add=", "delete=", "list", "version"])
		
		if len(opts) == 0:
			# Adding without -a or --add: iptables 1.2.3.4 2.3.4.5
			if len(args) > 0:
				opts = []
				for arg in args:
					opts.append(("-a", arg))
			# Display help if no args exist
			else:
				opts = [("-h","")]
		
		for opt, arg in opts:
			if opt in ("-h", "--help"):
				help()
			elif opt in ("-v", "--version"):
				print version()
			elif opt in ("-a", "--add"):
				subprocess.call(["iptables", "-A", "INPUT", "-s", arg, "-j", "DROP"])
			elif opt in ("-d", "--delete"):
				subprocess.call(["iptables", "-D", "INPUT", "-s", arg, "-j", "DROP"])
			elif opt in ("-l", "--list"):
				subprocess.call(["iptables", "-L", "-n"])
	except getopt.GetoptError, err:
		print "blockip error: " + str(err)
		sys.exit(2)
	except:
		print "blockip error (unknown)."
		sys.exit(2)

def version():
	global _name, _version
	return _name + " " + _version

def help():
	print version()
	print "Its called a hustle honey"

if __name__ == "__main__":
	main(sys.argv[1:])
