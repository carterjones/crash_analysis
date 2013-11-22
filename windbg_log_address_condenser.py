import operator
import os
import re
import sys

'''
Takes in a directory containing the many output files of windbg crashes.
Outputs a list of addresses, along with log files that correspond to that
crash address.
'''

class CrashInfo:
  '''Information about a crash, obtained from the output of windbg'''
  def __init__(self):
    self.registers = {}
    self.stack_trace = []
    self.filename = ''

  def crash_address(self):
    if 'eip' in self.registers:
      return self.registers['eip']
    elif 'rip' in self.registers:
      return self.registers['rip']
    else:
      raise 'No instruction pointer was found.'

def usage(script_name):
  print 'usage: ' + script_name + ' <crash_log_directory>'

if __name__ == '__main__':
  # Verify that a parameter has been passed to the program.
  script_name = sys.argv[0]
  if len(sys.argv) != 2:
    usage(script_name)
    exit(1)

  crash_dir = sys.argv[1]
  if not os.path.isdir(crash_dir):
    print crash_dir + ' is not a valid directory'
    exit(1)

  addresses = {}
  address_lines = {}
  for filename in os.listdir(crash_dir):
    # Analyze each file.
    with open(crash_dir + os.sep + filename, 'r') as crash_file:
      # Prepare to work with data.
      data = crash_file.read()
      lines = data.split('\n')
      ci = CrashInfo()
      ci.filename = filename

      # Get registers.
      for line in lines:
        if 'ip=' in line or 'ax=' in line:
          registers_tmp = [reg for reg in line.split(' ') if reg.find('=') == 3]
          for reg in registers_tmp:
            reg_parts = reg.split('=')
            reg_name = reg_parts[0]
            reg_value = reg_parts[1]
            ci.registers[reg_name] = reg_value

      # Set address of crash.
      address = ci.crash_address()

      # Get the instruction that caused the crash.
      instruction_line = [x for x in lines if x.startswith(address)][0]
      address_lines[address] = instruction_line

      # Get the stack trace.
      r = re.compile('Stack Trace:.*?\n\n', re.MULTILINE|re.DOTALL)
      m = r.search(data)
      ci.stack_trace = m.group()[len('Stack Trace:\n'):-2].split('\n')[:-1]

      # Associate the file with the crash address.
      if address in addresses:
        addresses[address] = addresses[address] + [crash_dir + os.sep + filename]
      else:
        addresses[address] = [crash_dir + os.sep + filename]

  addresses_count = map(lambda x: (x, len(addresses[x]), addresses[x]), addresses)
  sorted_addresses = sorted(addresses_count, key=lambda x: x[1], reverse=True)
  for address,count,files in sorted_addresses:
    print address_lines[address] + ':'
    for f in files:
      print '  ' + f
