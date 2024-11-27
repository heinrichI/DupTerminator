using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.Checkers;
using Xunit;

namespace DupTerminator.UnitTest
{
    public class CheckNotBestBookNameTest
    {
        [Theory]
        [InlineData("Толстой Л.В._Война и Мир", true)]
        [InlineData("Сляднева_О_В_Особенности_организации_психологической_подготовки", true)]
        [InlineData("ГОСТ_42_4_16_2023_ГО_Приспособление_заглубленных_помещений_для_укрытия", false)]
        [InlineData("Организация,_вооружение_и_тактика_действий_частей_и_подразде_иностранных", false)]
        [InlineData("Щербак_В_В_Организация,_вооружение_и_тактика_действий_частей_и_подразделений", true)]
        [InlineData("Дроны_и_их_пилотирование_С_чего_начать_2021", false)]
        [InlineData("Басов А. Большой каталог роботов", true)]
        public void StartFromWriterTest(string fileName, bool contain)
        {
            Assert.Equal(contain, CheckNotBestBookName.StartFromWriter(fileName));
        }
        
        [Theory]
        [InlineData("2_Trofimov_Puli_mira", true)]
        [InlineData("Book 1 Twen", true)]
        [InlineData("Puli", true)]
        [InlineData("Книга", false)]
        [InlineData("159126750_2023_07_05_17_11_42_ПАМЯТКА_УЧАСТНИКАМ", false)]
        [InlineData("5128635729_2023-08-29__16_42_10_remont_kamaz_[tfile.ru].pdf", true)]
        public void OnlyLatinTest(string fileName, bool contain)
        {
            Assert.Equal(contain, CheckNotBestBookName.OnlyLatin(fileName));
        }
    }
}