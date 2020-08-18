# SM630-Fingerprint-Commands

The file *SM630Commands.cs* allows you to communicate with the SM630 Fingerprint module in your C#/Arduino based projects.

### NOTE: 
1. This is a C# implementation of the Arduino commands for the SM630 fingerprint module. 
2. An understanding of the SM630 fingerprint Arduino library is recommended. 
3. An understanding of C# is recommended so as to modify the .cs file to suit your need.

The module communicates via the serial port.
Most PCs do not come with serial ports again. However, a USB to serial converter can be used instead.

## Available Commands
1. **bool AddFingerprint(int id)**: Adds a fingerprint template to a specified ID. Returns true if success else false.
2. **bool DeleteFingerprint(int id)**: Deletes stored fingerprint template at a specified ID. Returns true if success else false.
3. **bool EmptyDatabase()**: Deletes all saved fingerprint templates on the device. Returns true if success else false.
4. **bool UploadTemplate(int id, byte[] templ)**: Uploads an already acquired template to a specified ID on the module. Returns true if success else false.
5. **string ToHex(int value)**: Converts value to hexadecimal string to be used by the fingerprint module
6. **int FromHex(string value)**: Converts hexadecimal string back to hex.
