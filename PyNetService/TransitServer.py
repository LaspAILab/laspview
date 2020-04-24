#!/home10/program/anaconda3/bin/python
# -*- coding: utf-8 -*-
#Created by syf, Feb.14th 2020
#Tribute to Dr.Li WL
"""
Transit Server of LaspView File Transport 
Mainly run on node1, listen and wait for file from Local & storage4
And transfer file between them. 
(Limited to the fact that only node1 has network card and can run python)

Containing: 
1. A Server to listen Local file
2. A File manager for storage template arc file from Local on /home/LaspViewTransit/template
3. A Server to listen Remote file from Remote computer like storage4
4. A Client to send file to Local Server
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
    <sig>%sig</sig> #signal including stop server, change IP
    
    or
    <path>%path</paht> #pwd of file
</laspview>
"""
#ip & port of client is in the socket

#!/usr/bin/env python
#import socket
import re
import os
import signal
import threading
import asyncio
import datetime
BUFIZ = 1024
TEMP = "./Temp/"

##liu-node1
EXTERNAL_IP = '10.158.134.241'
EXTERNAL_PORT = 9538
INTERNAL_IP = '192.168.5.201'
INTERNAL_PORT = 9539

#EXTERNAL_IP = '127.0.0.1'
#EXTERNAL_PORT = 9538
#INTERNAL_IP = '127.0.0.1'
#INTERNAL_PORT = 9539

LOCAL_PORT=9540
#ADDR = (IP,PORT)
  
def addTagWithAttr(msgList, tag, attr):
    """
    msgList is List of message, waiting to join at last
    tag & attrbt is tag & attribute in XML format
    """
    alist=["".join(["<", tag, " ", attr, ">"])]
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
    
class TransitError(BaseException):
    def __init__(self, msg):
        self.msg = msg
    def __str__(self):
        print("TransitError"+self.msg)
        return "TransitError"+self.msg


class Server(threading.Thread):
#class Server():
    """
    Base Class of InternalServer & ExternalServer
    Supply a basic lister for Message
    msgProcess() is a empty function, waiting to be rewriten after inherit
    """
    def __init__(self,threadName, addr, port):
        """Start Server amd wait"""
        super(Server,self).__init__(name = threadName)
        self.name = threadName
        self.userAddr = dict()
        self.addrLogChangeTime = 0
        self.serverAddr = addr
        self.serverPort = port
        
#        asyncio.create_task(self.asyncServerStart(ADDR[0], ADDR[1]))

#        self.listner = socket.socket(socket.AF_INET,socket.SOCK_STREAM)
#        self.listner.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
#        self.listner.bind(ADDR)
#        self.listner.listen(5)
#
#        
    def run(self):
        #asyncio.run(self.asyncServerStart(self.serverAddr, self.serverPort))
        #asyncio.create_task(self.main())
        self.loop = asyncio.new_event_loop()
        asyncio.set_event_loop(self.loop)

        coro  = asyncio.start_server(
            self.asyncServerHandler,self.serverAddr, self.serverPort)
        self.server = self.loop.run_until_complete(coro)

        addr = self.server.sockets[0].getsockname()
        print(f'Serving on {addr}')
        self.loop.run_forever()
#    async def main(self):
#        while True:
#            try:
#                print (self.name + 'waiting for connection...')
#                self.connection,addr = self.listner.accept()
#                print ('...connected from:'+str(addr))
#               

#
#            except ConnectionRefusedError as e:
#                print ('Receive ConnectionRefusedError:'+str(e.args))
#            else:
#                print("receive msg succeed for %s %s"%addr)
#            finally:
#                self.connection.close()
#                print(str(addr)+'client down')
#                
#            try:
##                if self.msgProcess("".join(msgList)) == False:
##                    break
#                msg = "".join(msgList)
#                task = asyncio.create_task(self.msgProcess(msg))
##                task.add_done_callback(self.msgProcess_callback)
#            except ConnectionRefusedError as e:
#                print ('MsgProcess ConnectionRefusedError:'+str(e.args))
#                
#
#    async def msgProcess(self):
###        pass
###    
##    def msgProcess_callback(self):
###        if task.result() == False:
###            self.listner.close()
###            print("server has stop")
##        return
###        return task.result()
#    
#    def sendMsg(self, msg, addr):
#        """msg is a string"""
#        try:
#            clientSocket = socket.socket(socket.AF_INET,socket.SOCK_STREAM)
#            clientSocket.connect(addr)
#            print ('connect to '+str(addr))
#            clientSocket.send(msg.encode())
#        finally:
#            clientSocket.close()
#        
#    def sendFile(self,path,addr,user):
#        try:
#            clientSocket = socket.socket(socket.AF_INET,socket.SOCK_STREAM)
#            clientSocket.connect(addr)
#            print ('connect to '+str(addr))
#            
#            s = re.split("[\\\/]",path)
#            nam = s[-2]+'-'+s[-1]
#            typ = nam.split(".")[-1]
#            clientSocket.send(("<laspview user=%s>"%user+"<file name=%s type=%s>"%(nam,typ)).encode())
#            f = open(path,'rb')
#            while True:
#                msg = f.read(4096)
#                if not msg:
#                   break
#                clientSocket.send(msg)
#            f.close()
#            clientSocket.send("</file></laspview>".encode())
#            print ('send file success')        
#        finally:
#            clientSocket.close()

    """Method for Server / Receive Message"""
    async def asyncServerStart(self):
        coro = asyncio.start_server(
            self.asyncServerHandler,self.serverAddr, self.serverPort)
        self.server = await coro 
        addr = self.server.sockets[0].getsockname()
        print(f'Serving on {addr}')
    
       # async with self.server:
       #     await self.server.serve_forever()
        
   
    async def asyncServerHandler(self, reader, writer):
        try:
            errMsg = None
            
            data = await reader.read()
            message = data.decode()
            addr = writer.get_extra_info('peername')
            print(f"Received from {addr!r}")
        
            rtn = await self.msgProcess(message)
#            await asyncio.sleep(100)
            if rtn == False:
                writer.write(("<laspview user=echo><RtnMsg>Server start to close</RtnMsg></laspview>").encode())
                self.server.close()
                await self.server.wait_closed()
#                self.loop.close()  # 终止事件循环
                print("Server has STOP")
                #in Linux
                os.kill(os.getpid(), signal.SIGKILL)#kill himself 

            elif type(rtn) == str:
                writer.write(("<laspview user=echo><RtnMsg>"+rtn+"</RtnMsg></laspview>").encode())
                writer.write_eof()
                await writer.drain()
            
            elif type(rtn) == list:
                for buf in rtn:
                    writer.write(buf)
                writer.write_eof()
                print("succeed to send back.")   
                
        except TimeoutError as e:
            errMsg =  "TransitError: Network Failure. Plz Check Network Condition"+str(e)
            print(errMsg)
        except ConnectionRefusedError as e:
            errMsg = "TransitError: Link Succeed but Reused. Plz Check Network Condition"+str(e)
            print(errMsg)
        except FileNotFoundError as e:
            errMsg = "TransitError: Unknown Path. "+str(e)
            print(errMsg)
        except TransitError as e:
            errMsg = e.msg
            print(e)

        except BaseException as e:
            errMsg = str(e.args)
            print((type(e),(e.args)))
        finally:
            if errMsg != None:
                try:
                    print(errMsg)
                    writer.write(("<laspview user=echo><ErrMsg>"+errMsg+"</ErrMsg></laspview>").encode())
                    writer.write_eof()
                    await writer.drain()
                except BaseException:
                    pass
            writer.close()
            print("Server connection is closed")
        

    async def msgProcess(self, msg):
        pass
    
    
    """Method for Client / Sebd Message"""
    async def asyncClientSendMsg(self, message, user, port):
        addr = self.getUserAddr(user)
        if addr == None:
            return
        reader, writer = await asyncio.open_connection(
            addr, port)
        print(f'Send: {message!r}')
        writer.write(message.encode())
        writer.write_eof()
        await writer.drain()
        print('Close the connection')
#        await data = reader.read()12
        writer.close()    
    
    async def asyncClientSendFile(self, path, user, port):
        addr = self.getUserAddr(user)
        if addr == None:
            return
        reader, writer = await asyncio.open_connection(
            addr, port)
    
        s = re.split("[\\\/]",path)
        nam = s[-2]+'-'+s[-1]
        typ = nam.split(".")[-1]
        writer.write(("<laspview user=%s>"%user+"<file name=%s type=%s>"%(nam,typ)).encode())
        
        f = open(path,'rb')
        while True:
            msg = f.read(BUFIZ)
            if not msg:
                  break
            writer.write(msg)
        f.close()
        
        writer.write("</file></laspview>".encode())
        await writer.drain()
        print ('send file %s success'%(nam+typ))        
        writer.close()

        
    def getUserAddr(self, user):
        self.checkUserAddr()
        addr = self.userAddr.get(user, None)
        if addr == None:
            msg = "TransitError: No IP Address for user %s, plz check username and use checkIP signal in local LaspView First"%user
            raise TransitError(msg)
        else:
            return addr
    
    def readUserAddr(self):
        with open("UserAddress.log", 'r') as fp:
            lines = fp.readlines()
        for line in lines:
            m = re.match("([a-zA-Z0-9]{3,10}):(((?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?))", line)
            if m != None:
                self.userAddr[m.group(1)]=m.group(2)
                
    def writeUserAddr(self):
        lines = []
        for key in self.userAddr.keys():
            lines.append(key+":"+self.userAddr[key]+"\n")
        with open("UserAddress.log", 'w') as fp:
            fp.writelines(lines)
    
    def changeUserAddr(self, user, addr):
        if not user in self.userAddr.keys() or self.userAddr[user] != addr:
            self.userAddr[user]=addr
            self.writeUserAddr()
        else:
            return
        
    def checkUserAddr(self):
        if not os.path.isfile("UserAddress.log"):
            with open("UserAddress.log", 'w'):
                pass
        mtime = os.path.getmtime("UserAddress.log")
        if mtime > self.addrLogChangeTime:
            self.readUserAddr()
            self.addrLogChangeTime = mtime

class InternalServer(Server):
    def __init__(self,threadName, addr, port):
        super(InternalServer,self).__init__(threadName, addr, port)


    async def msgProcess(self, msg):
        msg, tag, attr = rmTag(msg)

        """First tag: laspview"""
        if tag == "laspview":
            
            user = attr.get("user", None)
            """ tag laspview should contain attribute user which is in UserList
                otherwise return"""
            if user == None:
                print("unknown user: %s"%msg)
                raise TransitError("unknown user: %s"%msg)
            if os.path.isdir(TEMP+user) == False:
                os.makedirs(TEMP+user)
 
            msg, tag, attr = rmTag(msg)
            
            
            if tag == "sig":
                
                if msg == "StopServer":
                    print("STOP Server now...")
                    return False
                
                elif msg == "ls":
                    print("ls for "+user)
                    fldir = os.listdir(TEMP+user)
                    if len(fldir) == 0:
                        raise TransitError("Empty!")
                    fldir = sorted(fldir,
                                   key=lambda x: os.path.getmtime(os.path.join(TEMP+user, x)))
                    return "     ".join(fldir)
                        
                elif msg == "clear":
                    print("clear for "+user)
                    if os.path.isdir(TEMP+user) == False:
                        os.makedirs(TEMP+user)
                    fldir = os.listdir(TEMP+user)
                    for file in fldir:
                        os.remove(TEMP+user+"/"+file)
                    return "All file in template is cleared"
                
                elif re.match("cp", msg):
                    print("start to cp " + " ".join(msg.split()[1:]))
                    rtn = []
                    rtn.append("<laspview user=echo><CpRtn>".encode())
                    fldir = os.listdir(TEMP+user)
                    files = []
                    for f in fldir:
                        for pattern in msg.split()[1:]:
                            if re.match(pattern,f) and f not in files:
                                files.append(f)
                    if len(files) == 0:
                        raise TransitError("No File Match!")
                    for f in files:
                        rtn.append(("<file name=%s>"%file).encode())
                        f = open(TEMP+user+"/"+file,'rb')
                        while True:
                            buf = f.read(BUFIZ)
                            if not buf:
                                  break
                            rtn.append(buf)
                        f.close()
                        rtn.append("</file>".encode())
                    rtn.append("</CpRtb></laspview>".encode())
                    return rtn
                
                elif re.match("cl", msg):
                    print("start to cp latest file")
                    rtn = []
                    rtn.append("<laspview user=echo><CpRtn>".encode())
                    fldir = os.listdir(TEMP+user)
                    if len(fldir) == 0: 
                        raise TransitError("Empty!")
                    fldir = sorted(fldir, 
                                   key=lambda x: os.path.getmtime(os.path.join(TEMP+user, x)))
                    for i in range(0, int(msg.split(" ")[1])):
                        rtn.append(("<file name=%s>"%fldir[-i-1]).encode())
                        f = open(TEMP+user+"/"+fldir[-1],'rb')
                        while True:
                            buf = f.read(BUFIZ)
                            if not buf:
                                break
                            rtn.append(buf)
                        f.close()
                        rtn.append("</file>".encode())
                    rtn.append("</CpRtb></laspview>".encode())
                    return rtn
                
#            elif tag == "file":
#                typ = attr.get("type")
#                nam = attr.get("name")
#                if typ and nam:
#                    msgList = addTagWithAttr([msg], tag,
#                                             "type="+typ+" name="+nam)
#                    msgList = addTagWithAttr(msgList, "laspview",
#                                            "user="+user)
#                    msg = "".join(msgList)
#                    
##                    self.sendMsg(msg, (userAddr,LOCAL_PORT))
#                    await self.asyncClientSendMsg(msg, user, LOCAL_PORT)
#                    print("send file %s to %s local client"%(nam,user))
#                else:
#                    print("wrong file message: no name or type")
                  
            
            elif tag == "path":
#                self.sendFile(msg, (userAddr,LOCAL_PORT),user)
                files = msg.split()
                if len(files) >= 10:
                    print("too many files")
                    return("too many files")
                elif len(files) == 1:
                    await self.asyncClientSendFile(files[0], user, LOCAL_PORT)
                    print("Send %s to %s_local Successfully."%(files[0],user))
                    return("Send %s to %s_local Successfully."%(files[0],user))
                elif len(files) > 1:
                    for f in files:
                        await self.asyncClientSendFile(f, user, LOCAL_PORT)
                    print("Send %d files to %s_local Successfully."%(len(files),user))
                    return "Send %d files to %s_local Successfully."%(len(files),user)
            
            else:
                print("unknown tag in InternalServer.msgProcess():\n %s"%msg)
                return("unknown tag in InternalServer.msgProcess():\n %s"%msg)
            
        else:
            print("unknown message: %s"%msg)
            return("unknown message: %s"%msg)
        
        
class ExternalServer(Server):
    def __init__(self, threadName, addr, port):
        super(ExternalServer,self).__init__(threadName, addr, port)
        
    async def msgProcess(self,msg):
        msg, tag, attr = rmTag(msg)
        """First tag: laspview"""
        if tag == "laspview":
            user = attr.get("user", None)
            """ tag laspview should contain attribute user which is in UserList
                otherwise return"""
            if user == None:
                print("unknown user: %s"%msg)
                return ("unknown user: %s"%msg)
            
            msg, tag, attr = rmTag(msg)
            if tag == "sig": 
                if re.match(r"^CheckIP.*", msg):
                    remoteIP = re.sub("^CheckIP=", "", msg, count=1)
                    self.readUserAddr()
                    self.changeUserAddr(user, remoteIP)
                    print("change ip of %s to %s"%(user,remoteIP))
                    return "IP received"
                    
            if tag == "file":
                if os.path.isdir(TEMP+user) == False:
                    os.makedirs(TEMP+user)
                filename = attr.get("name", None)
                if filename == None:
                    filename = datetime.datetime.now().strftime('%m%d%H%M%S.xx')
                with open(TEMP+user+"/"+filename, 'w') as wfile:
                    wfile.write(msg)
                print("Receive %s from %"%(filename, user))
                return "File received"
    
    
class timer():
    pass    


if __name__ == "__main__" :
#    InternalServer((INTERNAL_IP, INTERNAL_PORT))

#    a = InternalServer("namt",(INTERNAL_IP, INTERNAL_PORT))

    a = InternalServer("LaspViewInternalServer",INTERNAL_IP, INTERNAL_PORT)
    b = ExternalServer("LaspViewExternalServer",EXTERNAL_IP, EXTERNAL_PORT)

    a.start()
    b.start()

    
    



""""
A Simple example of asyncio task and its call back
coppy it to a new file and run in IPython
"""
#import asyncio
#import time
#import functools
#
#async def say_after(delay, what):
#    await asyncio.sleep(delay)
#    print(what)
#
#async def main():
#    task1 = asyncio.create_task(
#        say_after(1, 'hello'))
#
#    task2 = asyncio.create_task(
#        say_after(2, 'world'))
#
#    print(f"started at {time.strftime('%X')}")
#
##    # Wait until both tasks are completed (should take
##    # around 2 seconds.)
#    await task1
#    await task2
##
##    print(f"finished at {time.strftime('%X')}")
#def callback(task):
#    print(task.result())
#
#if __name__ == "__main__":
#    task = asyncio.create_task(main())
#    task.add_done_callback(callback)

