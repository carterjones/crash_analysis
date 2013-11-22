import math
import operator
import os
import re
import sys

'''
Takes in a directory containing the many output files of windbg crashes.
Outputs a list of addresses, along with log files that correspond to that
crash address.
'''

def get_cyclic_rating(value):
  '''
  Rates the string with a value depending on how much repetition occurs in the
  string.

  The rating system is not intensley complex, but it ranks highly repetetive
  strings very highly.
  '''
  if not isinstance(value, basestring):
    return 0
  if len(value) == 8 or len(value) == 16:
    weight = 0
    # Count the number of occurances of each byte in the value.
    pairs = [value[0:2], value[2:4], value[4:6], value[6:8]]
    if len(value) == 16:
      pairs = pairs + [value[8:10], value[10:12], value[12:14], value[14:16]]
    pairs = [p for p in pairs if '00' not in p]
    pairs_unique = list(set(pairs))
    for pair in pairs_unique:
      num_occurrances = value.count(pair)
      if num_occurrances > 1:
        weight += int(math.pow(2, num_occurrances))
    return int(weight/2)
    '''
    # Look for repeated single characters.
    for x in value:
      # Check if it is repeated 16 times, then 8 times, then 4, etc.
      if x * 16 in value:
        weight += 8
      elif x * 8 in value:
        weight += 4
      elif x * 4 in value:
        weight += 2
      elif x * 2 in value:
        weight += 1
    '''
    # Look for repeated double characters.
    pairs = [value[0:2], value[2:4], value[4:6], value[6:8]]
    if len(value) == 16:
      pairs = pairs + [value[8:10], value[10:12], value[12:14], value[14:16]]
    for x in pairs:
      # Check if it is repeated 8 times, then 4, etc.
      if x * 8 in value:
        weight += 8
      elif x * 4 in value:
        weight += 4
      elif x * 2 in value:
        weight += 2
    # Look for repeated four characters.
    fours = [value[0:4], value[4:8]]
    if len(value) == 16:
      fours = fours + [value[8:12], value[12:16]]
    for x in fours:
      # Check if it is repeated 4 times and then 2 times.
      if x * 4 in value:
        weight += 8
      elif x * 2 in value:
        weight += 4
    # Return the weight.
    return weight

  else:
    print '[-] ' + value + ' can not be rated for cyclic patterns'
    return 0

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

  def score():
    pass

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
