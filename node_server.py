import os
from SimpleXMLRPCServer import SimpleXMLRPCServer
from SimpleXMLRPCServer import SimpleXMLRPCRequestHandler
import socket
from subprocess import Popen, PIPE
import threading
import time
import xmlrpclib

controller_rpc_url = 'http://192.168.139.1:8000'

def delayed_exit():
  time.sleep(1)
  os._exit(0)

class FuzzingNode:
  def execute_command(self, cmd):
    process = Popen(cmd, stdout=PIPE)
    (output, err) = process.communicate()
    exit_code = process.wait()
    return output

  def close(self):
    # Call this as a delayed exit, so that the controller gets a response and
    # does not hang.
    t = threading.Thread(target=delayed_exit)
    t.daemon = True
    t.start()

  def ping(self):
    return True

def register_fuzzing_node():
  # Wait for server to start listening on this node.
  time.sleep(0.5)
  s = xmlrpclib.ServerProxy(controller_rpc_url)
  s.register_fuzzing_node(local_ip_address)

# Create server
local_ip_address = socket.gethostbyname(socket.gethostname())
server = SimpleXMLRPCServer((local_ip_address, 8000),
                            requestHandler=SimpleXMLRPCRequestHandler,
                            allow_none=True)
server.register_introspection_functions()
server.register_instance(FuzzingNode())

# Register with controller
t = threading.Thread(target=register_fuzzing_node)
t.daemon = True
t.start()

# Run the server's main loop
server.serve_forever()
