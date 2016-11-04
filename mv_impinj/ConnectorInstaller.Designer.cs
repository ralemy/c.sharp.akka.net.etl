namespace mv_impinj
{
    partial class ConnectorInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.connectorProccessInstaller1 = new mv_impinj.ConnectorProccessInstaller();
            // 
            // connectorProccessInstaller1
            // 
            this.connectorProccessInstaller1.Account = System.ServiceProcess.ServiceAccount.LocalService;
            this.connectorProccessInstaller1.Password = null;
            this.connectorProccessInstaller1.Username = null;
            // 
            // ConnectorInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.connectorProccessInstaller1});

        }

        #endregion

        private ConnectorProccessInstaller connectorProccessInstaller1;
    }
}