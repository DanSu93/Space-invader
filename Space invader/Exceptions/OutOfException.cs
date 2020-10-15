using System;

namespace spaceInvader
{
	// TODO: имя файла не то
    class InvalidSpaceObjectException : Exception
    {
        public InvalidSpaceObjectException(string message): base(message)
        {

        }
    }
}
