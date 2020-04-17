#! /home10/program/anaconda3/bin/python 
# -*- coding: utf-8 -*-
"""
Often used python Script for transfer file to Local, 
and grab file from template directory

Containing:
1. A File grabber to catch file from /home/LaspViewTransit/template on node1,
   which is managed by TransitServer,  to running directory
2. A remote Client to send file to Transit Server
"""

"""
Messege XML Format
<laspview user="%username">
    <file type="%type" name="%name">
        # %type = arc  
        # %name = directory name of file
        %fileContent
    </file>
    
    or
    <sig>%sig</sig> #signal including stop server, grab file
    
    or
    <path>%path</paht> #pwd of file
</laspview>
"""

import re
import os
import asyncio
import argparse


"""CHANGEABLE PARAMETERS for USER"""
USER = "shiyf"
TEMP = './template'




#HOST = '127.0.0.1'
#liu-node1
HOST = '192.168.5.201'
PORT = 9539
BUFSIZ = 1024




def clientSend(msg, addr, port):
    """msg is a string"""
<<<<<<< HEAD
    loop = asyncio.get_event_loop()
    loop.run_until_complete(asyncSendMsg(msg, addr, port))
    loop.close()    
=======
    asyncio.ensure_future(asyncSendMsg(msg, addr, port))

>>>>>>> change some UI
   
async def asyncSendMsg(msg, addr, port):
    try:
        reader, writer = await asyncio.open_connection(
            addr, port)
        #print(f"connect to {addr!r}:{port!r}")
        writer.write(msg.encode())
        writer.write_eof()
        await writer.drain()
    
        data = await reader.read()
        rtnMsg = data.decode()
        msgProcess(rtnMsg)
        #print("client closed")
        writer.close()
    except ConnectionRefusedError as e:
        print(e)
    
def msgProcess(msg):
    msg, tag, attr = rmTag(msg)
    
    """First tag: laspview"""
    if tag == "laspview":
        user = attr.get("user", None)
        """ tag laspview should contain attribute user which is in UserList
            otherwise return"""
        if user == None:
            print("unknown user: %s"%msg)
            return
        elif user == "echo":
            msg, tag, attr = rmTag(msg)
            if tag == "RtnMsg":
                print(msg)
    
            elif tag == "ErrMsg":
                print("\033[41;1m"+msg+"\033[0m")
                
            elif tag == "CpRtn":
                fmatch = re.findall(r"<file ([^/<>]*)>([^/<>]*)</file>",msg)
                for file in fmatch:
                    name = file[0].split('=')[1]
                    with open(name,"w") as fp:
                        fp.write(file[1])
                    print("cp succeed: "+name)
            else:
                print("unknown tag %s"%tag)
    else:
        print("unknown message: %s"%msg)
        return
                
def addTagWithAttr(msgList, tag, attrbt):
    """
    msgList is List of message, waiting to join at last
    tag & attrbt is tag & attribute in XML format
    """
    alist=["".join(["<", tag, " ", attrbt, ">"])]
    alist.extend(msgList)
    alist.append("</"+tag+">")
    """return like: ['<laspview user=shiyf>', 'helloworld', '</laspview>'] """
    return alist
        
def addTag(msgList, tag):
    """
    add tag with no attribute
    msgList is List of message, waiting to join at last
    tag & attrbt is tag & attribute in XML format
    """
    alist=["".join(["<", tag, ">"])]
    alist.extend(msgList)
    alist.append("</"+tag+">")
    """return like: ['<laspview user=shiyf>', 'helloworld', '</laspview>'] """
    return alist

def rmTag(msg):
    """"get tag name and attributes, than remove tag contents"""
    st = re.match(r"<([^/<>]*)>",msg)
    if st == None:
        print("wrong XML message")
        print(msg)
        return(msg, None, None)
    st = st.group(1).split()
    tag  = st.pop(0)
    attr = dict()
    if len(st)!=0:
        for at in st:
            at = at.split("=")
            attr[at[0]]=at[1]
    msg = re.sub(r"^(<%s([^>/])*>)|(</%s>)$"%(tag,tag), "", msg)
    return (msg, tag, attr)
#def sendFile(pwd,fname):
#    try:
#        clientSocket = socket.socket(socket.AF_INET,socket.SOCK_STREAM)
#        clientSocket.connect(ADDR)
#        print ('connect to '+str(ADDR))
#        
#        s = pwd.split("/|\\")
#        nam = s[-1]+'-'+fname
#        typ = fname.split(".")[-1]
#        clientSocket.send(("<laspview user=%s>"%USER+"<file name=%s type=%s>"%(nam,typ)).encode())
#        f = open(pwd+"\\"+fname,'rb')
#        while True:
#            msg = f.read(4096)
#            if not msg:
#               break
#            clientSocket.send(msg)
#        f.close()
#        clientSocket.send("</file></laspview>".encode())
#        print ('send file success')        
#    finally:
#        clientSocket.close()
    
    
    
def sendPath():
    pass


  
if __name__ == "__main__":

    parser = argparse.ArgumentParser(description='Script to Send Message To Local LaspView, and to Make Lasp Calculation Project',epilog='')
    parser.add_argument('-u',  "--user", type = str,  default = [USER], nargs = 1 ,
                        help = """Change default user to given argument, Default to $USER in the file.""")
    parser.add_argument('-s',  "--send", type = str, default = ["best.arc"],
                        help = "Path of file to send. Default option. Default to send best.arc")
    parser.add_argument('-ls', "--list",  action = "store_true",
                        help = "List the file in /$TEMP/$USER")
    parser.add_argument('-cp', "--copy",  type = str, nargs = '+',
                        help = "Copy file in /$TEMP/$USER to current dir. Allowed Py-style Regular expression ")
    parser.add_argument('-cl', "--copylatest",  action = "store_true",
                        help = "Copy latest received file to current dir.")
    parser.add_argument('-m',  "--make",  action = "store_true",
                        help = "Make LASP computation project")
    parser.add_argument('-kill',  "--kill",  action = "store_true",
                        help = "Ask TransitServer to kill himself")
    parser.add_argument('-clear', "--clear",  action = "store_true",
                        help = "remove  all file in /$TEMP/$USER  ")

    args = parser.parse_args()

    print(args)
    
    if args.list == True:
        msgList = addTag(["ls"],"sig")
        msgList = addTagWithAttr(msgList,"laspview","user="+args.user[0])
        clientSend("".join(msgList),HOST,PORT)
    
    elif args.copylatest != None:
        msg = "cl " 
        msgList = addTag(msg,"sig")
        msgList = addTagWithAttr(msgList,"laspview","user="+args.user[0])
        clientSend("".join(msgList),HOST,PORT)
        
    elif args.copy != None:
        msg = "cp "+ " ".join(args.copy) 
        msgList = addTag(msg,"sig")
        msgList = addTagWithAttr(msgList,"laspview","user="+args.user[0])
        clientSend("".join(msgList),HOST,PORT)
    
    elif args.clear == True:
        msgList = addTag(["clear"],"sig")
        msgList = addTagWithAttr(msgList,"laspview","user="+args.user[0])
        clientSend("".join(msgList),HOST,PORT)
        
    elif args.kill == True:
        msgList = addTag(["StopServer"],"sig")
        msgList = addTagWithAttr(msgList,"laspview","user="+args.user[0])
        clientSend("".join(msgList),HOST,PORT)
    
    else:
        print("Start to send %s"%args.send)
        """send Path of filename to Transit Server"""
        if not os.path.isfile(args.send):
            print("Error: "+args.send + " not exist")
        else:
            msgList = addTag([os.getcwd()+"/"+args.send],"path")
            msgList = addTagWithAttr(msgList,"laspview","user="+args.user[0])
            clientSend("".join(msgList), HOST,PORT)

    
    
#   def grabLatest
