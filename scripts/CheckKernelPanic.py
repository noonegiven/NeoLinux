import os, sys, time
name = "sysKern.sysNeo"

def CheckKernelPanic():
    print("cwd: ", os.getcwd())
    print("files: ", os.listdir("."))
    if not os.path.exists(name):
        panic(f"Missing {name}!")
    else:
        print("Found files.")
        sys.exit(0)
def panic(msg):
    print("=== KERNEL PANIC ===")
    print(msg)
    sys.stdout.flush()
    while True:
        time.sleep(1)

if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] == "--check":
        CheckKernelPanic()