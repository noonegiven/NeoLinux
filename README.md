# NeoLinux
Custom C# based Linux masked operating system

# How to install

* Install *Alpine Linux* without a graphical interface.
* Install .NET 10.0
* Move "Neo_linux1.0.0" to a USB drive
* Rename "Neo_linux1.0.0" to "Neo"
* Connect a USB drive and run the following commands:

    > su -
    
    > mkdir -p /usr/local/bin
    
    > mount /dev/sda1 /mnt
    
    > cp /mnt/Neo /usr/local/bin/
    
    > chmod +x /usr/local/bin/Neo
    
    > mkdir /lib64 && ln -s /lib/ld-musl-x86_64.so.1 /lib64/ld-linux-x86-64.so.2
    
    > apk add --no-cache libstdc++ libgcc libgomp
    
    > ldconfig /lib /usr/lib
    
    > rm -rf /lib64
    
    > apk add --no-cache gcompat
    
    > export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
    
    > echo "export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1" >> /etc/profile
    
    > source /etc/profile
    
    > apk add --no-cache icu-libs
    
  > sed -i 's|tty1::.*\|tty1::once:/bin/sh -c "/usr/local/bin/NeoLinux; /sbin/poweroff"\|g' /etc/inittab
  
    
* Reboot and it should run straight into NeoOS
