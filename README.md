# PrintServer

**Problem:** Cant print directly to printer from a browser based application (always needs a preview popup).

**Purpose:** Web App will send file for printing to a desktop application for direct printing. 

**Todos:**
- [x] Listening
- [x] Responding
- [x] Receiving File
- [x] Printing to selected printer (PDF)
- [ ] Printing to selected printer (TXT)

**Sample Endpoint:**
http://localhost:49956/?filename=C:\Data.pdf

**Response:**
- **not found** - if filename not found locally
- **ok** - file received
- **ready** - if no parameters

