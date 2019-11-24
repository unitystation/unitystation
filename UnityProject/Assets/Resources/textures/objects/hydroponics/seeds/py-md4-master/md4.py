from struct import pack
from binascii import hexlify


def make_words(byte_array):

    res = []

    for i in xrange(0, len(byte_array), 4):

        index = i/4
        res.append(byte_array[i+3])
        res[index] = (res[index] << 8) | byte_array[i+2]
        res[index] = (res[index] << 8) | byte_array[i+1]
        res[index] = (res[index] << 8) | byte_array[i]

    return res
        


def md4(message, debug=False):
    """
    https://tools.ietf.org/html/rfc1320
    """

    # we'll need to remember this for later
    original_length = len(message)

    message = [ord(c) for c in message]

    # add a '1' bit via a byte
    message += [0x80]

    mod_length = len(message) % 64
    # padding to 448 % 512 bits (56 % 64 byte)
    if mod_length < 56:
        message += [0x00] * (56 - mod_length)
    else:
        message += [0x00] * (120 - mod_length)

    # add the length as a 64 bit big endian, use lower order bits if length overflows 2^64
    length = [ord(c) for c in pack('>Q', (original_length * 8) & 0xFFFFFFFFFFFFFFFF)]

    # add the two words least significant first
    message.extend(length[::-1])

    if debug:
        print ("\nafter padding {0}".format([[hex(b) for b in message]]))

    # initialize the registers to magic values
    A = 0x67452301
    B = 0xefcdab89
    C = 0x98badcfe
    D = 0x10325476

    # define F, G, and H
    def F(x,y,z): return ((x & y) | ((~x) & z))
    def G(x,y,z): return (x & y) | (x & z) | (y & z)
    def H(x,y,z): return x ^ y ^ z

    # round functions
    def FF(a,b,c,d,k,s): return ROL((a + F(b,c,d) + X[k]) & 0xFFFFFFFF, s)
    def GG(a,b,c,d,k,s): return ROL((a + G(b,c,d) + X[k] + 0x5A827999) & 0xFFFFFFFF, s)
    def HH(a,b,c,d,k,s): return ROL((a + H(b,c,d) + X[k] + 0x6ED9EBA1) & 0xFFFFFFFF, s)

    # define a 32-bit left-rotate function (<<< in the RFC)
    def ROL(x, n): return ((x << n) & 0xFFFFFFFF) | (x >> (32-n))

    # turn the padded message into a list of 32-bit words
    M = make_words(message)
        
    # process each 16 word (64 byte) block
    for i in xrange(0, len(M), 16):

        X = M[i:i+16]

        # save the current values of the registers
        AA = A
        BB = B
        CC = C
        DD = D

        if debug:
            print ("\n")
            print ("A (initial): {0}".format(hex(A)))
            print ("B (initial): {0}".format(hex(B)))
            print ("C (initial): {0}".format(hex(C)))
            print ("D (initial): {0}".format(hex(D)))

        # round 1

        # perform the 16 operations
        A = FF(A,B,C,D,0,3)
        D = FF(D,A,B,C,1,7)
        C = FF(C,D,A,B,2,11)
        B = FF(B,C,D,A,3,19)

        A = FF(A,B,C,D,4,3)
        D = FF(D,A,B,C,5,7)
        C = FF(C,D,A,B,6,11)
        B = FF(B,C,D,A,7,19)

        A = FF(A,B,C,D,8,3)
        D = FF(D,A,B,C,9,7)
        C = FF(C,D,A,B,10,11)
        B = FF(B,C,D,A,11,19)

        A = FF(A,B,C,D,12,3)
        D = FF(D,A,B,C,13,7)
        C = FF(C,D,A,B,14,11)
        B = FF(B,C,D,A,15,19)

        if debug:
            print ("\n")
            print ("A (round 1): {0}".format(hex(A)))
            print ("B (round 1): {0}".format(hex(B)))
            print ("C (round 1): {0}".format(hex(C)))
            print ("D (round 1): {0}".format(hex(D)))

        # round 2

        # perform the 16 operations
        A = GG(A,B,C,D,0,3)
        D = GG(D,A,B,C,4,5)
        C = GG(C,D,A,B,8,9)
        B = GG(B,C,D,A,12,13)

        A = GG(A,B,C,D,1,3)
        D = GG(D,A,B,C,5,5)
        C = GG(C,D,A,B,9,9)
        B = GG(B,C,D,A,13,13)

        A = GG(A,B,C,D,2,3)
        D = GG(D,A,B,C,6,5)
        C = GG(C,D,A,B,10,9)
        B = GG(B,C,D,A,14,13)

        A = GG(A,B,C,D,3,3)
        D = GG(D,A,B,C,7,5)
        C = GG(C,D,A,B,11,9)
        B = GG(B,C,D,A,15,13)

        if debug:
            print ("\n")
            print ("A (round 2): {0}".format(hex(A)))
            print ("B (round 2): {0}".format(hex(B)))
            print ("C (round 2): {0}".format(hex(C)))
            print ("D (round 2): {0}".format(hex(D)))

        # round 3

        A = HH(A,B,C,D,0,3)
        D = HH(D,A,B,C,8,9)
        C = HH(C,D,A,B,4,11)
        B = HH(B,C,D,A,12,15)

        A = HH(A,B,C,D,2,3)
        D = HH(D,A,B,C,10,9)
        C = HH(C,D,A,B,6,11)
        B = HH(B,C,D,A,14,15)

        A = HH(A,B,C,D,1,3)
        D = HH(D,A,B,C,9,9)
        C = HH(C,D,A,B,5,11)
        B = HH(B,C,D,A,13,15)

        A = HH(A,B,C,D,3,3)
        D = HH(D,A,B,C,11,9)
        C = HH(C,D,A,B,7,11)
        B = HH(B,C,D,A,15,15)

        if debug:
            print ("\n")
            print ("A (round 3): {0}".format(hex(A)))
            print ("B (round 3): {0}".format(hex(B)))
            print ("C (round 3): {0}".format(hex(C)))
            print ("D (round 3): {0}".format(hex(D)))

        # increment by previous values
        A =  ((A + AA) & 0xFFFFFFFF)
        B =  ((B + BB) & 0xFFFFFFFF)
        C =  ((C + CC) & 0xFFFFFFFF)
        D =  ((D + DD) & 0xFFFFFFFF)

        if debug:
            print ("\n")
            print ("A (incrmnt): {0}".format(hex(A)))
            print ("B (incrmnt): {0}".format(hex(B)))
            print ("C (incrmnt): {0}".format(hex(C)))
            print ("D (incrmnt): {0}".format(hex(D)))
            print ("\n")


    # convert endian-ness for output
    A = hexlify(pack('<L', A))
    B = hexlify(pack('<L', B))
    C = hexlify(pack('<L', C))
    D = hexlify(pack('<L', D))

    return A + B + C + D
