clc
clear all

% IP ve port bilgileri
ip = '192.168.1.33';
port = 1233;

% Mevcut UDP nesnelerini bulma ve kapatma
existingUDP = instrfind('LocalPort', port);
if ~isempty(existingUDP)
    fclose(existingUDP);
    delete(existingUDP);
end

% UDP nesnesini oluşturma
udpObj = udp(ip, port, 'LocalPort', port);

% UDP nesnesini açma
fopen(udpObj);

% Sunucuya test verisini gönderme
fprintf(udpObj, '255_255_255_255_255');

% Veriyi çekme ve işleme döngüsü
try
    while true
        % UDP'den veri okuma
        if udpObj.BytesAvailable > 0
            receivedMessage = fscanf(udpObj); % Satır bazında okuma

            % Mesajı 'Em=' ile kontrol etme
            if startsWith(receivedMessage, 'Em=')
                % EMG verilerini işleme
                ProcessEmgData_udp(receivedMessage);
            end
        end
    end
catch ME
    % Hata durumunda UDP nesnesini kapatma
    fclose(udpObj);
    delete(udpObj);
    rethrow(ME);
end

% UDP nesnesini kapatma
fclose(udpObj);
delete(udpObj);

% EMG verilerini işlemek için fonksiyon
function ProcessEmgData_udp(receivedMessage)
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
