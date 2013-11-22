import operator
import os
import re
import sys

'''
Takes in a directory containing the many output files of windbg crashes.
Outputs a list of addresses, along with log files that correspond to that
crash address.
'''

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

      # Get registers.
      registers = {}
      for line in lines:
        if 'ip=' in line or 'ax=' in line:
          registers_tmp = [reg for reg in line.split(' ') if reg.find('=') == 3]
          for reg in registers_tmp:
            reg_parts = reg.split('=')
            reg_name = reg_parts[0]
            reg_value = reg_parts[1]
            registers[reg_name] = reg_value

      # Set address of crash.
      address = None
      if 'eip' in registers:
        address = registers['eip']
      elif 'rip' in registers:
        address = registers['rip']
      else:
        raise 'instruction pointer address not found in crash log'

      # Get the instruction that caused the crash.
      instruction_line = [x for x in lines if x.startswith(address)][0]
      address_lines[address] = instruction_line

      # Get the stack trace.
      r = re.compile('Stack Trace:.*?\n\n', re.MULTILINE|re.DOTALL)
      m = r.search(data)
      stack_trace = m.group()[len('Stack Trace:\n'):-2].split('\n')[:-1]

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