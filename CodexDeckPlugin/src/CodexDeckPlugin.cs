namespace Loupedeck.CodexDeckPlugin
{
    using System;

    // This class contains the plugin-level logic of the Loupedeck plugin.

    public class CodexDeckPlugin : Plugin
    {
        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is a Universal plugin or an Application plugin.
        public override Boolean HasNoApplication => true;

        // Initializes a new instance of the plugin class.
        public CodexDeckPlugin()
        {
            // Initialize the plugin log.
            PluginLog.Init(this.Log);

            // Initialize the plugin resources.
            PluginResources.Init(this.Assembly);
        }

        // This method is called when the plugin is loaded.
        public override void Load()
        {
            ActionEditorCommands.AddAction(new ReviewTemplateEditorCommand());
            ActionEditorCommands.AddAction(new DebugTemplateEditorCommand());
            ActionEditorCommands.AddAction(new RefactorTemplateEditorCommand());
            DynamicCommands.AddAction(new ReviewWorkflowCommand());
            DynamicCommands.AddAction(new DebugWorkflowCommand());
            DynamicCommands.AddAction(new RefactorWorkflowCommand());
            DynamicCommands.AddAction(new FastModeCommand());
            DynamicCommands.AddAction(new ContinueNewTaskCommand());
        }

        // This method is called when the plugin is unloaded.
        public override void Unload()
        {
        }
    }
}
