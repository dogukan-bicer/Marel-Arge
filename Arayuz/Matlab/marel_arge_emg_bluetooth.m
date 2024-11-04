clc
clear all
global emg_data_array emg_data2_array % Dizileri global olarak tanımla

% Başlangıç dizileri oluştur
emg_data_array = [];
emg_data2_array = [];

% Bluetooth cihazı bilgileri
bluetoothDevice = 'Marel Robotik'; % Cihaz adı
bluetoothPort = 1; % Cihazın kanal numarası

% Mevcut Bluetooth nesnelerini bulma ve kapatma
existingBluetooth = instrfind('Port', bluetoothPort);
if ~isempty(existingBluetooth)
    fclose(existingBluetooth);
    delete(existingBluetooth);
end

% Bluetooth nesnesini oluşturma
try
    btObj = bluetooth(bluetoothDevice, bluetoothPort);
catch ME
    disp('Bluetooth cihazına bağlanırken hata oluştu:');
    disp(ME.message);
    return; % Hata oluşursa işlemi sonlandır
end

% Bluetooth bağlantısını açma
try
    fopen(btObj);
catch ME
    disp('Bluetooth bağlantısı açılamadı:');
    disp(ME.message);
    return; % Hata oluşursa işlemi sonlandır
end

% Sunucuya test verisini gönderme
fprintf(btObj, '255_255_255_255_255');

% Veriyi çekme ve işleme döngüsü
try
    while true
        % Bluetooth'tan veri okuma
        if btObj.BytesAvailable > 0
            receivedMessage = fscanf(btObj); % Satır bazında okuma

            % Mesajı 'Em=' ile kontrol etme
            if startsWith(receivedMessage, 'Em=')
                % EMG verilerini işleme
                ProcessEmgData_bt(receivedMessage);
            end
        end
    end
catch ME
    % Hata durumunda Bluetooth nesnesini kapatma
    fclose(btObj);
    delete(btObj);
    rethrow(ME);
end

% Bluetooth nesnesini kapatma
fclose(btObj);
delete(btObj);

% EMG verilerini işlemek için fonksiyon
function ProcessEmgData_bt(receivedMessage)
    global emg_data_array emg_data2_array % Global dizilere erişim sağla
    try
        % '=' işaretinden sonrasını ayırma
        index = strfind(receivedMessage, '=') + 1;
        if ~isempty(index)
            emgString = receivedMessage(index:end);

            % '>' işaretinden ayırarak iki ayrı EMG verisi olarak bölme
            emgverisi = strsplit(emgString, '>');

            % EMG verilerini sayısal değerlere çevirme
            emg_data = str2double(emgverisi{1});
            emg_data2 = str2double(emgverisi{2});

            % Verileri global dizilere ekleme
            emg_data_array = [emg_data_array, emg_data];
            emg_data2_array = [emg_data2_array, emg_data2];

            % Workspace'e aktarma
            assignin('base', 'emg_data_array', emg_data_array);
            assignin('base', 'emg_data2_array', emg_data2_array);

            % Verileri konsolda görüntüleme
            fprintf('EMG Data 1: %d\n', emg_data);
            fprintf('EMG Data 2: %d\n', emg_data2);
        end
    catch ME
        disp('Veri işlenirken bir hata oluştu:');
        disp(ME.message);
    end
end
