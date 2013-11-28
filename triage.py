import os
import psutil
import shutil
import sys
import subprocess
import threading
import time

'''
Triages a directory of crash files with windbg, categorizing them in folders
according to exception type. All exceptions that occur have a log generated
and saved to disk.
'''

delete_crashes_that_do_not_cause_an_exception = True
cdb_path = 'c:\\Program Files\\Debugging Tools for Windows (x86)\\cdb.exe'
continue_killing_target_program = False

def usage(script_name):
  print 'usage: ' + script_name + ' <target_program> <crash_directory>'

def wait_and_kill_process(proc):
  print '[+] starting to wait...'
  max_time_to_sleep = 10
  time_slept = 0
  time_to_sleep = 0.1
  while time_slept < max_time_to_sleep and continue_killing_target_program:
    time.sleep(time_to_sleep)
    time_slept += time_to_sleep
  if time_slept < max_time_to_sleep:
    print '[+] wait time not reached. not killing thread'
  elif time_slept >= max_time_to_sleep and continue_killing_target_program:
    print '[+] wait time reached. killing thread.'
    process_name = target_program[target_program.rfind('\\')+1:]
    for proc in psutil.process_iter():
      try:
        if proc.name == process_name:
          proc.kill()
          print '[+] process killed'
          break
      except:
        pass

if __name__ == '__main__':
  if len(sys.argv) != 3:
    usage(sys.argv[0])
    exit(1)

  target_program = sys.argv[1]
  path_to_files = sys.argv[2]
  if not os.path.exists(target_program):
    print target_program + ' does not exist'
    exit(1)

  if not os.path.isdir(path_to_files):
    print path_to_files + ' is not a valid directory'
    exit(1)
  
  for filename in os.listdir(path_to_files):
    # run !exploitable
    args = [cdb_path, '-g', '-c', '!analyze -v;kv;.load msec; !exploitable -v; q', target_program, path_to_files + '\\' + filename]
    print ' '.join(args)
    process = subprocess.Popen(args, stdout=subprocess.PIPE)
    # start a new thread to kill the process if it is still open
    continue_killing_target_program = True
    t = threading.Thread(target=wait_and_kill_process, args=[process])
    t.daemon = True
    t.start()
    # continue monitoring windbg
    print '[+] communicating...'
    (output, err) = process.communicate()
    continue_killing_target_program = False
    print '[+] waiting...'
    exit_code = process.wait()
    process.stdout.close()
    print output
    # get the exploitability rating
    exploitability = [x for x in output.split('\n') if 'Exploitability Classification: ' in x]
    if len(exploitability) == 1:
      exploitability = exploitability[0][len('Exploitability Classification: '):]
      print exploitability
    else:
      continue
    if exploitability != 'NOT_AN_EXCEPTION':
      if not os.path.isdir(exploitability):
        os.makedirs(exploitability)
      shutil.move(path_to_files + '\\' + filename, exploitability + '\\' + filename)
      with open(exploitability + '\\' + filename + '.log', 'w') as windbg_out:
        windbg_out.write(output)
    else:
      if delete_crashes_that_do_not_cause_an_exception:
        os.remove(path_to_files + '\\' + filename)
