import glob
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

def get_byte_repetition_rating(value):
  '''
  Rates the string with a value depending on how much repetition of bytes
  occurs in the string.
  '''
  if not isinstance(value, basestring):
    return 0
  if len(value) == 8 or len(value) == 16:
    weight = 0
    # Count the number of occurances of each byte in the value.
    pairs = [value[0:2], value[2:4], value[4:6], value[6:8]]
    if len(value) == 16:
      pairs = pairs + [value[8:10], value[10:12], value[12:14], value[14:16]]
    # Exclude null bytes.
    pairs = [p for p in pairs if '00' not in p]
    # Get a unique set of the bytes.
    pairs_unique = list(set(pairs))
    # count the occurances and add them to the weight.
    for pair in pairs_unique:
      num_occurrances = value.count(pair)
      if num_occurrances > 1:
        weight += int(math.pow(2, num_occurrances))
    return int(weight/2)
  else:
    print '[-] ' + value + ' can not be rated for cyclic patterns'
    return 0

class CrashInfo:
  '''Information about a crash, obtained from the output of windbg'''
  def __init__(self):
    self.registers = {}
    self.stack_trace = []
    self.filepath = ''
    self.offset_considered_abnomrally_large = 65536
    self.possible_stack_corruption = False
    self.crash_instruction_line = ''

  def __str__(self):
    return '(' + str(self.score()) + ') ' + self.filepath

  def populate_info(self, crash_log_filepath):
    self.filepath = crash_log_filepath
    with open(self.filepath, 'r') as crash_file:
      # Prepare to work with data.
      data = crash_file.read()
      lines = data.split('\n')

      # Get registers.
      for line in lines:
        if 'ip=' in line or 'ax=' in line:
          registers_tmp = [reg for reg in line.split(' ') if reg.find('=') == 3]
          for reg in registers_tmp:
            reg_parts = reg.split('=')
            reg_name = reg_parts[0]
            reg_value = reg_parts[1]
            self.registers[reg_name] = reg_value

      # Get the instruction that caused the crash.
      instruction_line = [x for x in lines if x.startswith(self.crash_address())][0]
      self.crash_instruction_line = instruction_line

      # Get the stack trace.
      r = re.compile('Stack Trace:.*?\n\n', re.MULTILINE|re.DOTALL)
      m = r.search(data)
      self.stack_trace = m.group()[len('Stack Trace:\n'):-2].split('\n')[:-1]

  def crash_address(self):
    if 'eip' in self.registers:
      return self.registers['eip']
    elif 'rip' in self.registers:
      return self.registers['rip']
    else:
      raise Exception('No instruction pointer was found. Crash file: ' + self.filepath)

  def check_for_stack_corruption(self):
    # Perform scoring based on the stack trace.
    if self.stack_trace:
      top_call = self.stack_trace[0]

      # If the top of the stack could not be resolved, it could imply a stack
      # corruption, which could be interesting.
      if top_call is 'Unknown':
        self.possible_stack_corruption = True

      # If the top of the stack has a very large offset, it could indicate
      # that a coincidentally valid address was called, due to overwriting EIP,
      # which could be interesting.
      elif '+0x' in top_call:
        # Get the offset value.
        offset = int(re.search('\+0x.*', top_call).group()[1:], 16)
        if offset >= self.offset_considered_abnomrally_large:
          self.possible_stack_corruption = True

    # Return true if a possible stack corruption has been encountered.
    return self.possible_stack_corruption

  def score(self):
    score = 0

    # Rate the byte repetition within the registers. Higher repetition
    # indicates possibly more interesting results.
    for reg in self.registers:
      score += get_byte_repetition_rating(self.registers[reg])

    # Check for a stack coruption.
    self.check_for_stack_corruption()
    if self.possible_stack_corruption:
      score += 3

    return score

def print_crashes_sorted_by_crash_address(crashes):
  crash_groups = {}
  for ci in crashes:
    crash_address = ci.crash_address()
    if crash_address in crash_groups:
      crash_groups[crash_address] = crash_groups[crash_address] + [ci]
    else:
      crash_groups[crash_address] = [ci]
  for address in sorted(crash_groups, key=lambda k: len(crash_groups[k]), reverse=True):
    print crash_groups[address][0].crash_instruction_line + ':'
    for ci in crash_groups[address]:
      print '  ' + ci.filepath

def print_crashes_sorted_by_score(crashes):
  crashes.sort(key=operator.methodcaller('score'), reverse=True)
  for ci in crashes:
    print ci

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

  crashes = []
  for filepath in glob.glob(crash_dir + os.sep + '*.log'):
    ci = CrashInfo()
    ci.populate_info(filepath)
    crashes.append(ci)

  print_crashes_sorted_by_crash_address(crashes)
  #print_crashes_sorted_by_score(crashes)