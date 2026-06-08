VC ESAPI plugins:  IsoSplit
1. Description
This script is designed to streamline the workflow for creating FIN plans for TBI treatments. Its purpose is to generate a separate FIN plan for each isocenter, with the corresponding fields copied from the source plan.

2. Instructions
Note: General rule of the user interface: if the font is black, the field is selectable. If the font is light gray, it is information text. 
Note: The “Run Script” button is enabled only when the script can run. If selections are incomplete, the button is disabled and unclickable. This is noticeable because the button is not highlighted when the mouse hovers over it.
The script is designed to fulfill two scenarios: 2 plans with head-first (HF) and feet-first (FF) orientation of the planning image, and a single plan containing all isocenters:
•	If two plans are selected, isocenters split in each plan separately: beam isocenters with a positive z-coordinate are retained from the HF plan, and those with a negative z-coordinate are retained from the FF plan.
•	For the single plan scenario, all isocenters are retained without distinguishing between positive and negative z-coordinates. To proceed with a single plan scenario, check the relevant checkbox.
If an SGRT plan (isocenter at user origin, single static beam) is needed, it can be added by checking the relevant checkbox, which is selected by default.
Course ID populated automatically but can be adjusted to the user’s preference. The limit is 16 characters.
Populated isocenter group names are used for plan IDs and reference point IDs, based on the rules read from the XML scheme (see the expected output paragraph for more details). These IDs are also adjustable in the user interface and limited to 10 characters. Highlighted in blue when selected. To each isocenter a CBCT field can be added automatically by selecting the relevant checkbox.
3. Expected Output
The script creates one plan per isocenter by copying the source plan and removing all beams that do not belong to the current isocenter. This preserves the calculated dose.
Additional behavior:
•	Reference points are added to the patient record and must be manually added to each plan.
•	Dose limits are set to a twice-daily treatment schedule.
•	DRRs are generated for the created setup beams using a CT HU threshold of (100-1000).
•	Plan IDs and reference point naming conventions rely on beam IDs. If the logic cannot resolve them, it falls back to default IDs (“Plan” or “Plan_NP”). These rules are stored in a separate XML file located in a folder next to the .dll file. The rules can be updated without the need to reapprove the script. 
•	If a plan ID already exists in the course, a numeric suffix is added to ensure uniqueness.
•	If a reference point with the same ID exists, the script does not create a duplicate.
•	In cases of extended SSD treatment, the user must manually create the reference point for the second isocenter per treatment region.
4. Troubleshooting
The script provides a condensed, real-time progress report within its user interface.
Log files are created for each script execution and stored in the “Logs” folder located in the same directory as the plugin .dll file.
 

 
