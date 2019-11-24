from md4 import md4

Filename = "seeds_seed-poppy.png"


f = open(Filename, 'r+')
contents = f.read()
f.close()

print ("[+] " + md4(contents))
