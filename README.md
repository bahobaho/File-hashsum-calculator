A console application calculating the SHA-256 hash (feel free to use System.Security.Cryptography) of all files in a specified directory and subdirectories.

Command line parameters:
> calchash.exe <input_directory_name> <ouptut_file_name>

Output is a text file containing the following:
- hashes of each file in the input directory
- Performance in MB/s by CPU time

Output file format:
<hash 1> <file name 1>
<hash 2> <file name 2>
...
<hash n> <file name n>
Performance: <value> MB/s (by CPU time)
