clc
clear all

% Seri port bilgileri
serialPort = 'COM10'; % Kendi seri port numaranıza göre değiştirin
baudRate = 115200; % Seri port hızını ayarlayın

% Mevcut seri port nesnelerini bulma ve kapatma
existingPorts = instrfind('Port', serialPort);
if ~isempty(existingPorts)
    fclose(existingPorts);
    delete(existingPorts);
end

% Seri port nesnesini oluşturma
serialObj = serial(serialPort, 'BaudRate', baudRate);

% Seri portu açma
fopen(serialObj);

% Veriyi çekme ve işleme döngüsü
try
    while true
        % Seri porttan veri okuma
        if serialObj.BytesAvailable > 0
            receivedMessage = fscanf(serialObj); % Satır bazında okuma

            % Mesajı 'Em=' ile kontrol etme
            if startsWith(receivedMessage, 'Em=')
                % EMG verilerini işleme
                ProcessEmgData_usb(receivedMessage);
            end
        end
    end
catch ME
    % Hata durumunda seri portu kapatma
    fclose(serialObj);
    delete(serialObj);
    rethrow(ME);
end

% Seri portu kapatma
fclose(serialObj);
delete(serialObj);

% EMG verilerini işlemek için fonksiyon
function ProcessEmgData_usb(receivedMessage)
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

            % Verileri workspace'e kaydetme
            assignin('base', 'emg_data', emg_data);
            assignin('base', 'emg_data2', emg_data2);

            % Verileri konsolda görüntüleme
            fprintf('EMG Data 1: %d\n', emg_data);
            fprintf('EMG Data 2: %d\n', emg_data2);
        end
    catch ME
        disp('Veri işlenirken bir hata oluştu:');
        disp(ME.message);
    end
end
