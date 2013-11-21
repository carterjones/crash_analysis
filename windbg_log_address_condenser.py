import operator
import os
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
    with open(crash_dir + os.sep + filename, 'r') as crash_file:
      data = crash_file.read()
      lines = data.split('\n')
      address = None
      try:
        address = [x for x in lines if 'eip=' in x][0][len('eip='):len('eip=')+8]
      except:
        address = [x for x in lines if 'rip=' in x][0][len('rip='):len('rip=')+16]
      instruction_line = [x for x in lines if x.startswith(address)][0]
      address_lines[address] = instruction_line
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
