import os

f = r'AsistenciaDesktop_v2\Data\SyncEngine.cs'
with open(f, 'r', encoding='utf-8') as file:
    content = file.read()

# Replace the variables
content = content.replace('private static bool isSyncing = false;', 'private static Task _currentSyncTask;')

old_method = '''        public static async Task SyncDataAsync()
        {
            if (isSyncing) return;
            isSyncing = true;

            try
            {'''

new_method = '''        public static Task SyncDataAsync()
        {
            if (_currentSyncTask != null && !_currentSyncTask.IsCompleted)
                return _currentSyncTask;
            
            _currentSyncTask = DoSyncAsync();
            return _currentSyncTask;
        }

        private static async Task DoSyncAsync()
        {
            try
            {'''

content = content.replace(old_method, new_method)

content = content.replace('''            finally
            {
                isSyncing = false;
            }''', '''            finally
            {
            }''')

with open(f, 'w', encoding='utf-8') as file:
    file.write(content)
