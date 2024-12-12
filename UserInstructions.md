### Instructions to Use the TIA DB Reader Executable

Follow these steps to use the TIA DB Reader application. **Ensure you run the executable with administrator privileges** to allow seamless interaction with the TIA Portal.

1. **Prepare the Environment:**
   - Place the `DBs.txt` file in the `DBs` folder located in the same directory as the executable.
     - The `DBs.txt` file should list the Data Block (DB) names and their custom keys, one per line, in the following format:
       ```
       GLOBAL,64
       CH1,10
       FM1,20
       ```

2. **Prepare the TIA Projects Folder:**
   - Create a folder named `TiaProjects` in the same directory as the executable.
   - Place all the TIA Portal project files (e.g., `.ap19`) into this folder. The program will scan this folder recursively.

3. **Run the Executable:**
   - Start the executable file (`TIA_DB_Reader.exe`) as an administrator:
     - Right-click on the executable.
     - Select **Run as Administrator**.

4. **Workflow Execution:**
   - The program will:
     1. Scan the `TiaProjects` folder to discover all TIA Portal projects.
     2. Launch TIA Portal in headless mode (without GUI).
     3. Accept the TIA Portal API connection prompt:
        - A popup from TIA Portal will appear asking for API connection approval.
        - Select **Yes to All** to enable the connection.
     4. Process each project, retrieve all PLCs, and search for the DBs listed in `DBs.txt`.
     5. Export the found DBs into the `Exports` folder under project-specific and PLC-specific subfolders.
     6. Consolidate all comments extracted from the exported DBs into a single Excel file named `Consolidated_AlarmList.xlsx`, located in the `CompleteAlarmList` folder under `Exports`.

5. **Review the Output:**
   - Navigate to the `Exports` folder to review the results:
     - **Exported DBs:** Organized in project and PLC subfolders under `ExportedDBs`.
     - **Consolidated Comments:** Found in `CompleteAlarmList\Consolidated_AlarmList.xlsx`.

6. **Error Handling:**
   - If any errors occur during execution, check the console log for details.

7. **Repeat Usage:**
   - Before re-running the program, ensure that the `Exports` folder is cleared automatically or cleaned manually to avoid processing stale data.

---

### Important Notes
- The executable relies on the TIA Openness API. Ensure you have the necessary Siemens software and licenses installed.
- The program is designed to handle multiple projects and PLCs efficiently while respecting the DB order defined in `DBs.txt`.
- For any missing dependencies or issues, refer to the project documentation or contact support.

Enjoy using the TIA DB Reader application!
