import argparse
import os
import shutil
import subprocess

'''
notes

targets should be configured as follows:
- manual:
  - disable firewall
  - disable simple file sharing
  - create user:admin, password:1
- automate this:
  - install python 2.7
    - install psutil
  - install windbg
    - copy !exploitable to the root directory of windbg
  - enable automatic login (http://support.microsoft.com/kb/315231)
  - install autoit
'''

default_username = 'admin'
default_password = '1'
ip_list = ['192.168.91.128',
           '192.168.139.148',
           '192.168.139.149',
           '192.168.139.150']
print_debug_info = False
show_program_output = False

def execute_local_command(command):
  if print_debug_info:
    print command
  outfile = None
  if not show_program_output:
    outfile = open(os.devnull, 'w')
  return_code = subprocess.call(command, stdout=outfile, stderr=outfile)
  if not show_program_output:
    outfile.close()
  return return_code

def psexec(command, ip, username=default_username, password=default_password):
  full_command = 'psexec \\\\' + ip + ' -u ' + username + ' -p ' + password + ' ' + command
  return execute_local_command(full_command)

def start_fuzzing(username=default_username, password=default_password):
  for ip in ip_list:
    print 'Copying over start_minifuzz.au3 and stop_minifuzz.au3 on ' + str(ip) + '...'

    # Create the fuzzing_tools directory on the target system if it does not
    # exist.
    psexec('-w c:\ -d cmd /c mkdir fuzzing_tools', ip, username, password)

    # Share the directory on the remote system.
    psexec('net share shared="C:\\fuzzing_tools"', ip, username, password)

    # Connect a volume on localhost to the directory on the remote system.
    execute_local_command('net use z: \\\\' + ip + '\\shared /user:' + username + ' ' + password)

    # Copy start_minifuzz.au3 and stop_minifuzz.au3 to remote system.
    shutil.copyfile('start_minifuzz.au3', 'Z:\\start_minifuzz.au3')
    shutil.copyfile('stop_minifuzz.au3', 'Z:\\stop_minifuzz.au3')

    # Delete the volume on localhost that is connected to the directory on the
    # remote system.
    execute_local_command('net use z: /delete')

    # Unshare the directory on the remote system.
    psexec('net share shared /delete', ip, username, password)
    
    # Start the fuzzer.
    print 'Starting fuzzer...'
    psexec('-i -d "C:\\Program Files\\AutoIt3\\AutoIt3.exe" "C:\\fuzzing_tools\\start_minifuzz.au3"', ip, username, password)

def stop_fuzzing(username=default_username, password=default_password):
  for ip in ip_list:
    print 'Stopping fuzzer on ' + str(ip) + '...'
    psexec('-i "C:\\Program Files\\AutoIt3\\AutoIt3.exe" "C:\\fuzzing_tools\\stop_minifuzz.au3"', ip, username, password)

def run_triager(username=default_username, password=default_password):
  for ip in ip_list:
    print 'Copying over triage.py on ' + str(ip) + '...'

    # Create the fuzzing_tools directory on the target system if it does not
    # exist.
    psexec('-w c:\ -d cmd /c mkdir fuzzing_tools', ip, username, password)

    # Share the directory on the remote system.
    psexec('net share shared="C:\\fuzzing_tools"', ip, username, password)

    # Connect a volume on localhost to the directory on the remote system.
    execute_local_command('net use z: \\\\' + ip + '\\shared /user:' + username + ' ' + password)

    # Copy triage.py to remote system.
    shutil.copyfile('triage.py', 'Z:\\triage.py')

    # Delete the volume on localhost that is connected to the directory on the
    # remote system.
    execute_local_command('net use z: /delete')

    # Unshare the directory on the remote system.
    psexec('net share shared /delete', ip, username, password)
    
    # Start the fuzzer.
    print 'Running triage.py...'
    psexec('-w C:\\fuzzing_tools -i C:\\python27\\python.exe C:\\fuzzing_tools\\triage.py "C:\\Program Files\\VideoLAN\\VLC\\vlc.exe" C:\\minifuzz\\crashes', ip, username, password)

def restart_machine(username=default_username, password=default_password):
  for ip in ip_list:
    # Authenticate against the target machine.
    print 'Authenticating against ' + ip + '...'
    execute_local_command('net use \\\\' + ip + '\\IPC$ /user:' + username + ' ' + password)

    # Restart the machine.
    print 'Sending restart command...'
    execute_local_command('shutdown /m \\\\' + ip + ' /r /f /t 0')

if __name__ == '__main__':
  parser = argparse.ArgumentParser()
  parser.add_argument("--start",
                      help="start fuzzing",
                      action="store_true")
  parser.add_argument("--stop",
                      help="stop fuzzing",
                      action="store_true")
  parser.add_argument("--triage",
                      help="run the triager",
                      action="store_true")
  parser.add_argument("-r",
                      "--restart_machine",
                      help="restart the machine(s)",
                      action="store_true")
  parser.add_argument("-d",
                      "--debug",
                      help="show debugging information (sets -v automatically)",
                      action="store_true")
  parser.add_argument("-v",
                      "--verbose",
                      help="show output of programs executed by this script",
                      action="store_true")
  args = parser.parse_args()

  if args.verbose or args.debug:
    show_program_output = True

  if args.debug:
    print_debug_info = True

  if args.start:
    start_fuzzing()
  elif args.stop:
    stop_fuzzing()
  elif args.triage:
    run_triager()
  elif args.restart_machine:
    restart_machine()
  else:
    parser.print_help()
